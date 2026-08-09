# 🎮 TeamSpeak Overlay Pro v1.1.0-Alpha

[![Framework](https://img.shields.io/badge/.NET-8.0--WPF-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-v1.1.0--Alpha-orange.svg)](https://github.com/SergeyIvanovPro/teamspeak-overlay/releases)
[![Lineage II](https://img.shields.io/badge/Lineage%20II-Supported-00E5FF)](https://github.com/SergeyIvanovPro/teamspeak-overlay)
[![Design](https://img.shields.io/badge/UI-Material%20Design%203-6750A4)](https://m3.material.io/)

Современный, производительный и визуально безупречный голосовой оверлей для **TeamSpeak 3** и **TeamSpeak 6**, созданный специально для игроков **Lineage II** и командных геймеров.

---

## ✨ Ключевые возможности

- 🎮 **Авто-считывание ника персонажа Lineage II**:
  Оверлей автоматически считывает имя вашего персонажа из заголовка игрового окна Lineage II (`LU4 - CharacterName`) и выводит его на плашку в шапке (с возможностью переключения на Telegram `@handle`).
- ⚡ **Dual Engine (TS3 ClientQuery & TS6 WebSocket)**:
  Автоматическое сканирование и подключение как к классическому **TeamSpeak 3 ClientQuery** (порт 25639), так и к новому **TeamSpeak 6 WebSocket API** (порт 5899).
- 🎨 **Material Design 3 & Glassmorphism**:
  Премиальный современный интерфейс с плавающей стеклянной панелью, плавными анимациями и поддержкой светлой/темной тем.
- 🟢 **Кастомизация цвета говорящего игрока**:
  Выбор акцентного цвета подсветки ника говорящего соклановца из палитры Tailwind (Neon Cyan, Emerald Green, Electric Violet, Crimson Red, Gold Yellow).
- ⏱️ **Формат времени и подсказки**:
  Отображение часов (12ч/24ч) и 0ms мгновенные всплывающие подсказки при наведении на любой элемент настроек.
- 🔔 **Уведомления и тычки (Poke & Text Alerts)**:
  Всплывающие Material 3 тост-уведомления со звуковым сопровождением при получении тычек (`👉 Poke`) и личных сообщений в TS.
- ⌨️ **Горячие клавиши (Hotkeys)**:
  - `Ctrl + Shift + O` — показать/скрыть оверлей.
  - `Ctrl + Shift + M` — включить/выключить микрофон (Mute).

---

## 🏛 Архитектура проекта

Проект построен с соблюдением принципов **Clean Architecture** и паттерна **MVVM**:

```text
TeamSpeakOverlay/
├── Application/         # Бизнес-логика и UseCases (Strict UseCase Architecture)
│   └── UseCases/       # GetHeaderBadgeInfoUseCase, UpdateSettingsUseCase и др.
├── Domain/              # Доменные сущности, контракты и интерфейсы
│   ├── Entities/       # AppVersion, ClientItem, ChannelInfo, OverlaySettings
│   └── Interfaces/     # ITeamSpeakClient, ISettingsRepository
├── Infrastructure/      # Сетевое взаимодействие, Win32 API и сканеры
│   ├── GameTracker/    # Отслеживание активного окна Lineage II (Win32 API)
│   └── TeamSpeak/      # TS3 ClientQuery & TS6 WebSocket сканер
├── ViewModels/          # MainViewModel, SettingsViewModel (WPF Data Binding)
└── Views/               # Material 3 WPF Окна (OverlayWindow, SettingsWindow)
```

## 📦 Установка и Сборка

### 🚀 Варианты установки
1. **Установочный файл Setup (Рекомендуется)**:
   Скачайте [`TeamSpeakOverlay-v1.1.0-Alpha-Setup.exe`](https://github.com/texport/teamspeak-overlay/releases) для автоматической установки с созданием ярлыков на рабочем столе и в меню "Пуск".
2. **Портативная версия (Portable ZIP)**:
   Скачайте [`TeamSpeakOverlay-v1.1.0-Alpha.zip`](https://github.com/texport/teamspeak-overlay/releases), распакуйте в любую папку и запускайте без установки.

---

## 🛠 Руководство по сборке из исходников

### Требования
- **ОС**: Windows 10 / 11 (x64)
- **SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Inno Setup 6** (для сборки инсталлятора `.exe`)

### Сборка

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/texport/teamspeak-overlay.git
   cd teamspeak-overlay
   ```

2. Соберите проект:
   ```bash
   dotnet build -c Release
   ```

3. Для публикации одиночного бинарника:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```

4. Сборка установочного Setup `.exe` (Inno Setup):
   ```powershell
   & "ISCC.exe" installer.iss
   ```

Готовые бинарники и инсталлятор `TeamSpeakOverlay-v1.1.0-Alpha-Setup.exe` генерируются в папку `bin/Release/net8.0-windows/`.

---

## 👤 Автор и Лицензия

- **Автор**: [@SergeyIvanovPro](https://github.com/SergeyIvanovPro)
- **Репозиторий**: [github.com/texport/teamspeak-overlay](https://github.com/texport/teamspeak-overlay)
- **Лицензия**: Распространяется под открытой лицензией [MIT](LICENSE).

