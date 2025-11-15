🌐 Languages: [English(current)](README.md) | [Русский](README.ru.md)

# Localization Manager

Modern WPF application for translating localization files with support for both free and paid translation options.

## Requirements

- .NET 8.0 SDK or higher
- Visual Studio 2022 or higher (or another editor with .NET support)
- For Google Translate API: Google Cloud API Key with Google Translate API enabled (optional)

## Building the Project

1. Open a terminal in the project folder
2. Run the following commands:
   ```bash
   dotnet restore
   dotnet build
   ```
3. To run:
   ```bash
   dotnet run
   ```
4. Or create an executable:
   ```bash
   dotnet publish -c Release
   ```

## Translation Provider Selection

The application supports three options:

### 🆓 LibreTranslate (Free)
- Completely free and open source
- No API key required
- Uses public translation servers
- Automatically tested when selected

### 🆓 MyMemory (Free)
- Free translation service
- No API key required
- Alternative free option to LibreTranslate

### 💰 Google Translate API (Paid)
- Requires API key from Google Cloud
- Higher translation accuracy
- Requires setup in Google Cloud Console

### Obtaining Google Translate API Key

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable "Cloud Translation API"
4. Go to "Credentials" → "Create Credentials" → "API Key"
5. Copy the generated API key

> 💡 **Tip**: The application has a built-in tooltip (ℹ️ icon) next to the API key field with detailed instructions.

## Usage

1. Launch the application
2. **Select translation provider:**
   - **LibreTranslate (Free)** - selected by default, ready to use without setup
   - **MyMemory (Free)** - alternative free option
   - **Google Translate API (Paid)** - enter API key and click "Test Connection"
3. Select the source localization file (e.g., `localization.json`) via the "Browse..." button
4. Select the path to save the translated file via the "Browse..." button
5. Select the target translation language from the dropdown list
6. Click "🚀 Start Translation" to begin the translation process

## Features

- 🎨 Modern and intuitive interface
- 🆓 **Free translation option via LibreTranslate** - no API key required
- 🆓 **Alternative free option via MyMemory** - no API key required
- 💰 Optional Google Translate API support for more accurate translation
- 🔑 Built-in tooltips for obtaining API key
- 📊 Real-time translation progress display with percentages
- 🔄 Automatically skips service keys (starting with `__`)
- 💾 Preserves JSON file structure
- 🌍 Updates language code in the `__LANG` field
- ⚡ Error handling with continued operation on individual translation failures
- ⏹️ Stop translation feature that saves partial results
- 💾 Configuration is saved automatically when closing the application

## Supported Languages

- Russian (ru)
- Ukrainian (uk)
- Polish (pl)
- German (de)
- French (fr)
- Spanish (es)
- Italian (it)
- Portuguese (pt)
- Chinese (zh)
- Japanese (ja)
- Korean (ko)

## Notes

- The translation process may take significant time for large files
- **LibreTranslate**: Free, but may be slower and less accurate for some languages
- **MyMemory**: Free alternative, may have character limits for long texts
- **Google Translate API**: Requires setup and may have limits/fees, but usually more accurate
- It is recommended to make backup copies of source files
- For large files, it is recommended to use Google Translate API due to better performance
- Configuration (file paths, selected language, selected provider) is saved automatically when closing the application

