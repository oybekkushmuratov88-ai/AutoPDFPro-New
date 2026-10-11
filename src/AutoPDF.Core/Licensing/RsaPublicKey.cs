using System;
using System.Security.Cryptography;
using System.Text;

namespace AutoPDF.Core.Licensing
{
    /// <summary>
    /// The server's RSA public key, used only to VERIFY licence signatures (the private key never leaves the
    /// administrator's Google account). Accepts the standard SubjectPublicKeyInfo form ("BEGIN PUBLIC KEY",
    /// as WebCrypto/OpenSSL export it), PKCS#1 ("BEGIN RSA PUBLIC KEY") or bare base64 DER. A tiny DER reader is
    /// used because .NET Framework 4.8 has no ImportSubjectPublicKeyInfo.
    /// </summary>
    public sealed class RsaPublicKey
    {
        private static readonly byte[] RsaEncryptionOid = { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };

        private RsaPublicKey(RSAParameters parameters, byte[] spki)
        {
            Parameters = parameters;
            SubjectPublicKeyInfo = spki;
            KeyId = ComputeKeyId(spki);
        }

        public RSAParameters Parameters { get; }

        /// <summary>DER bytes as published by the server (what <see cref="KeyId"/> is computed from).</summary>
        public byte[] SubjectPublicKeyInfo { get; }

        /// <summary>First 8 bytes of SHA-256(DER) in hex — the server computes the same value.</summary>
        public string KeyId { get; }

        public int KeySizeBits => Parameters.Modulus.Length * 8;

        public string ToBase64() => Convert.ToBase64String(SubjectPublicKeyInfo);

        public static bool TryParse(string text, out RsaPublicKey key)
        {
            key = null;
            try
            {
                key = Parse(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static RsaPublicKey Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Empty public key.");
            var sb = new StringBuilder();
            foreach (string raw in text.Replace("\r", string.Empty).Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("-----", StringComparison.Ordinal)) continue;
                sb.Append(line);
            }
            string b64 = sb.ToString().Replace('-', '+').Replace('_', '/');
            while (b64.Length % 4 != 0) b64 += "=";
            byte[] der = Convert.FromBase64String(b64);
            return FromDer(der);
        }

        public static RsaPublicKey FromDer(byte[] der)
        {
            var outer = new DerReader(der);
            DerReader seq = outer.ReadSequence();
            // SubjectPublicKeyInfo starts with an AlgorithmIdentifier SEQUENCE; PKCS#1 with an INTEGER.
            if (seq.PeekTag() == 0x30)
            {
                DerReader alg = seq.ReadSequence();
                byte[] oid = alg.ReadElement(0x06);
                if (!BytesEqual(oid, RsaEncryptionOid)) throw new FormatException("Not an RSA public key.");
                byte[] bits = seq.ReadElement(0x03);
                if (bits.Length < 2 || bits[0] != 0) throw new FormatException("Bad BIT STRING.");
                byte[] pkcs1 = new byte[bits.Length - 1];
                Buffer.BlockCopy(bits, 1, pkcs1, 0, pkcs1.Length);
                return new RsaPublicKey(ReadPkcs1(pkcs1), der);
            }

            RSAParameters p = ReadPkcs1(der);
            return new RsaPublicKey(p, der);
        }

        private static RSAParameters ReadPkcs1(byte[] der)
        {
            DerReader seq = new DerReader(der).ReadSequence();
            byte[] modulus = TrimLeadingZeros(seq.ReadElement(0x02));
            byte[] exponent = TrimLeadingZeros(seq.ReadElement(0x02));
            if (modulus.Length < 128) throw new FormatException("RSA key too small (< 1024 bits).");
            return new RSAParameters { Modulus = modulus, Exponent = exponent };
        }

        /// <summary>RSASSA-PKCS1-v1_5 with SHA-256 over the UTF-8 bytes of <paramref name="message"/>.</summary>
        public bool Verify(string message, string signatureBase64)
        {
            if (message == null || string.IsNullOrWhiteSpace(signatureBase64)) return false;
            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(signatureBase64.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            try
            {
                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportParameters(Parameters);
                    return rsa.VerifyData(Encoding.UTF8.GetBytes(message), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static string ComputeKeyId(byte[] spki)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(spki);
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static byte[] TrimLeadingZeros(byte[] value)
        {
            int i = 0;
            while (i < value.Length - 1 && value[i] == 0) i++;
            if (i == 0) return value;
            byte[] r = new byte[value.Length - i];
            Buffer.BlockCopy(value, i, r, 0, r.Length);
            return r;
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>Just enough DER to read SEQUENCE / INTEGER / OID / BIT STRING / NULL.</summary>
        private sealed class DerReader
        {
            private readonly byte[] _data;
            private int _pos;
            private readonly int _end;

            public DerReader(byte[] data, int start = 0, int length = -1)
            {
                _data = data ?? throw new ArgumentNullException(nameof(data));
                _pos = start;
                _end = length < 0 ? data.Length : start + length;
            }

            public int PeekTag()
            {
                if (_pos >= _end) throw new FormatException("Unexpected end of DER.");
                return _data[_pos];
            }

            public DerReader ReadSequence()
            {
                ReadHeader(0x30, out int start, out int length);
                _pos = start + length;
                return new DerReader(_data, start, length);
            }

            public byte[] ReadElement(int tag)
            {
                // Skip a NULL that may follow the algorithm OID.
                ReadHeader(tag, out int start, out int length);
                var value = new byte[length];
                Buffer.BlockCopy(_data, start, value, 0, length);
                _pos = start + length;
                if (tag == 0x06 && _pos < _end && _data[_pos] == 0x05) _pos += 2;
                return value;
            }

            private void ReadHeader(int expectedTag, out int start, out int length)
            {
                if (_pos + 2 > _end) throw new FormatException("Unexpected end of DER.");
                int tag = _data[_pos++];
                if (tag != expectedTag) throw new FormatException("DER tag " + tag + ", expected " + expectedTag + ".");
                int first = _data[_pos++];
                if (first < 0x80)
                {
                    length = first;
                }
                else
                {
                    int count = first & 0x7F;
                    if (count < 1 || count > 3 || _pos + count > _end) throw new FormatException("Bad DER length.");
                    length = 0;
                    for (int i = 0; i < count; i++) length = (length << 8) | _data[_pos++];
                }
                if (length < 0 || _pos + length > _end) throw new FormatException("DER length out of range.");
                start = _pos;
            }
        }
    }
}
