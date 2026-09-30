# Молниезащита — Tauri 2 + React + TypeScript

Десктопное приложение для расчёта и трёхмерного проектирования зон защиты
молниеотводов (РУ 9504тм-Т1, РН 34-301-388:2016). Техническое задание и
план работ — [docs/SPEC.md](docs/SPEC.md).

## Состояние

Выполнен шаг 1 плана (SPEC §7): проект создан, ядро перенесено, тесты
переведены на vitest.

- `src/core/lightning.ts` — ядро расчёта, **байт-в-байт** из `lightning-core`
  (sha256 `df43d542…9866f`). Не править без прогона тестов.
- `src/core/lightning.test.ts` — те же 86 проверок, что в `lightning-core`.
  Изменён только каркас: вместо `console.log` / `process.exit` каждая проверка
  `near` / `is` стала отдельным тестом vitest, разделы — `describe`. Входные
  данные, ожидаемые значения и допуски не менялись.

## Команды

```
npm install
npm test              # vitest: 86 тестов ядра
npm run test:watch    # vitest в режиме наблюдения
npm run tauri dev     # запуск приложения
npm run tauri build   # установщик
```

Для `tauri dev/build` нужен Rust toolchain и системные зависимости Tauri:
https://tauri.app/start/prerequisites/
