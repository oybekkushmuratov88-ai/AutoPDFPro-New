using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoPDF.Core.Localization;
using AutoPDF.Core.Models;
using AutoPDF.Core.Settings;
using Xunit;

namespace AutoPDF.Core.Tests
{
    [CollectionDefinition("Localization", DisableParallelization = true)]
    public class LocalizationCollection
    {
    }

    /// <summary>L.Current is global state: these tests run alone and always restore it.</summary>
    [Collection("Localization")]
    public class LocalizationTests
    {
        /// <summary>Every public text of L: parameterless properties plus every method called with sample arguments.</summary>
        private static List<(string Name, string Text)> AllTexts()
        {
            var texts = new List<(string, string)>();
            foreach (PropertyInfo p in typeof(L).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(string)))
            {
                texts.Add((p.Name, (string)p.GetValue(null)));
            }

            foreach (MethodInfo m in typeof(L).GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName && m.Name != "T" && m.Name != "F" && m.Name != "NativeName"))
            {
                object[] args = m.GetParameters().Select(p => Sample(p.ParameterType)).ToArray();
                texts.Add((m.Name, (string)m.Invoke(null, args)));
            }

            foreach (SheetStatus status in Enum.GetValues(typeof(SheetStatus)))
            {
                texts.Add(("StatusText." + status, L.StatusText(status)));
            }
            return texts;
        }

        private static object Sample(Type t)
        {
            if (t == typeof(string)) return "X";
            if (t == typeof(int)) return 3;
            if (t == typeof(double)) return 1.5;
            if (t == typeof(bool)) return true;
            if (t == typeof(SheetStatus)) return SheetStatus.Ready;
            return "obj";
        }

        private static IEnumerable<(string, string)> TextsIn(UiLanguage language)
        {
            UiLanguage previous = L.Current;
            try
            {
                L.Current = language;
                return AllTexts();
            }
            finally
            {
                L.Current = previous;
            }
        }

        [Theory]
        [InlineData(UiLanguage.Uzbek)]
        [InlineData(UiLanguage.Russian)]
        [InlineData(UiLanguage.English)]
        public void EveryTextExists(UiLanguage language)
        {
            List<(string Name, string Text)> texts = TextsIn(language).ToList();
            Assert.True(texts.Count > 200, "expected all texts to be discovered, found " + texts.Count);
            Assert.All(texts, t => Assert.False(string.IsNullOrWhiteSpace(t.Text), t.Name + " is empty"));
        }

        [Fact]
        public void RussianAndEnglishHaveNoUzbekApostrophes()
        {
            // Uzbek Latin uses ‘ (o‘, g‘); a Russian or English text containing it was pasted into the wrong slot.
            Assert.All(TextsIn(UiLanguage.Russian), t => Assert.DoesNotContain("‘", t.Item2));
            Assert.All(TextsIn(UiLanguage.English), t => Assert.DoesNotContain("‘", t.Item2));
        }

        [Fact]
        public void EnglishHasNoCyrillic()
        {
            Assert.All(TextsIn(UiLanguage.English), t => Assert.False(t.Item2.Any(c => c >= '\u0400' && c <= '\u04FF'), t.Item1 + ": " + t.Item2));
        }

        [Fact]
        public void RussianTextsAreRussian()
        {
            // Every Russian text with words (not just symbols/numbers/product names) contains Cyrillic letters.
            var exceptions = new HashSet<string> { "AppTitle", "Ok", "MethodApi", "ColNumber", "GroupLanguage", "EchoWarning", "EchoError", "ColWindows", "HostName", "CompatibilityHosts" };
            foreach ((string name, string text) in TextsIn(UiLanguage.Russian))
            {
                if (exceptions.Contains(name)) continue;
                bool hasLetters = text.Any(char.IsLetter);
                bool hasCyrillic = text.Any(c => c >= '\u0400' && c <= '\u04FF');
                bool technical = text.All(c => !char.IsLetter(c) || c < 128) && (text.Contains("PDF") || text.Contains("SysVar") || text.Contains("===") || text.Length < 4);
                if (hasLetters && !technical) Assert.True(hasCyrillic, name + ": " + text);
            }
        }

        [Fact]
        public void LanguagesDiffer()
        {
            Assert.Equal("Skanerlash", Get(UiLanguage.Uzbek, () => L.Scan));
            Assert.Equal("Сканировать", Get(UiLanguage.Russian, () => L.Scan));
            Assert.Equal("Scan", Get(UiLanguage.English, () => L.Scan));
            Assert.Equal("Tayyor", Get(UiLanguage.Uzbek, () => L.StatusText(SheetStatus.Ready)));
            Assert.Equal("Готов", Get(UiLanguage.Russian, () => L.StatusText(SheetStatus.Ready)));
            Assert.Equal("Ready", Get(UiLanguage.English, () => L.StatusText(SheetStatus.Ready)));
        }

        [Fact]
        public void NativeNamesAndAuthor()
        {
            Assert.Equal("O‘zbekcha", L.NativeName(UiLanguage.Uzbek));
            Assert.Equal("Русский", L.NativeName(UiLanguage.Russian));
            Assert.Equal("English", L.NativeName(UiLanguage.English));
            Assert.Equal("Kushmuratov Oybek", AppInfo.Author);
            Assert.Contains("Kushmuratov Oybek", Get(UiLanguage.Russian, () => L.Copyright));
        }

        [Fact]
        public void LanguageIsSavedInSettings()
        {
            string dir = Path.Combine(Path.GetTempPath(), "apdf-lang-" + Guid.NewGuid());
            var svc = new SettingsService(Path.Combine(dir, "settings.xml"));
            svc.Save(new PluginSettings { Language = UiLanguage.English });
            Assert.Equal(UiLanguage.English, svc.Load().Language);

            // Files written by v2.0.0 have no <Language> element: Uzbek stays the default.
            File.WriteAllText(Path.Combine(dir, "settings.xml"),
                "<?xml version=\"1.0\"?><AutoPdfSettings><OutputFolder>C:\\PDF</OutputFolder></AutoPdfSettings>");
            PluginSettings old = svc.Load();
            Assert.Equal(UiLanguage.Uzbek, old.Language);
            Assert.Equal("C:\\PDF", old.OutputFolder);
            Directory.Delete(dir, true);
        }

        [Theory]
        [InlineData("Uzbek", UiLanguage.Uzbek)]
        [InlineData("Russian", UiLanguage.Russian)]
        [InlineData("English", UiLanguage.English)]
        public void SettingsFileWrittenBySetupIsRead(string written, UiLanguage expected)
        {
            // installer\AutoPDFPro.nsi writes exactly this on a first installation: the language chosen for
            // Setup, every other setting stays at its default.
            string dir = Path.Combine(Path.GetTempPath(), "apdf-setup-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "settings.xml");
            File.WriteAllText(file,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<AutoPdfSettings>\r\n  <Language>" + written +
                "</Language>\r\n</AutoPdfSettings>\r\n");
            try
            {
                PluginSettings loaded = new SettingsService(file).Load();
                PluginSettings defaults = new PluginSettings().Normalize();
                defaults.Language = expected;

                Assert.Equal(expected, loaded.Language);
                Assert.Equal(ToXml(defaults), ToXml(loaded));
                Assert.Empty(Directory.GetFiles(dir, "settings.xml.broken-*"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private static string ToXml(PluginSettings settings)
        {
            using (var writer = new StringWriter())
            {
                new System.Xml.Serialization.XmlSerializer(typeof(PluginSettings)).Serialize(writer, settings);
                return writer.ToString();
            }
        }

        private static string Get(UiLanguage language, Func<string> text)
        {
            UiLanguage previous = L.Current;
            try
            {
                L.Current = language;
                return text();
            }
            finally
            {
                L.Current = previous;
            }
        }
    }
}
