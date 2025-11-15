using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Stalker2LocalizationManager
{
    public partial class MainWindow : Window
    {
        private GoogleTranslateService? _translateService;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Source Localization File"
            };

            if (dialog.ShowDialog() == true)
            {
                SourceFileTextBox.Text = dialog.FileName;
                UpdateTranslateButtonState();
            }
        }

        private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Target Localization File",
                FileName = "localization_translated.json"
            };

            if (dialog.ShowDialog() == true)
            {
                TargetFileTextBox.Text = dialog.FileName;
                UpdateTranslateButtonState();
            }
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateTranslateButtonState();
        }

        private void TestApiButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyBox.Password;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                StatusTextBlock.Text = "❌ Please enter API Key";
                MessageBox.Show("Please enter API Key", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _translateService = new GoogleTranslateService(apiKey);
            StatusTextBlock.Text = "🔄 Testing API connection...";
            TestApiButton.IsEnabled = false;

            Task.Run(async () =>
            {
                try
                {
                    var result = await _translateService.TestConnectionAsync();
                    Dispatcher.Invoke(() =>
                    {
                        TestApiButton.IsEnabled = true;
                        if (result)
                        {
                            StatusTextBlock.Text = "✅ API connection successful! Ready to translate.";
                            MessageBox.Show("API connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            StatusTextBlock.Text = "❌ API connection failed. Please check your API key.";
                            MessageBox.Show("API connection failed. Please check your API key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        UpdateTranslateButtonState();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TestApiButton.IsEnabled = true;
                        StatusTextBlock.Text = $"❌ Error: {ex.Message}";
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void UpdateTranslateButtonState()
        {
            TranslateButton.IsEnabled = !string.IsNullOrWhiteSpace(SourceFileTextBox.Text) &&
                                       !string.IsNullOrWhiteSpace(TargetFileTextBox.Text) &&
                                       !string.IsNullOrWhiteSpace(ApiKeyBox.Password) &&
                                       _translateService != null &&
                                       File.Exists(SourceFileTextBox.Text);
        }

        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_translateService == null)
            {
                MessageBox.Show("Please test API connection first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sourceFile = SourceFileTextBox.Text;
            var targetFile = TargetFileTextBox.Text;
            var selectedLanguage = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ru";

            if (!File.Exists(sourceFile))
            {
                MessageBox.Show("Source file does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TranslateButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            StatusTextBlock.Text = "📂 Loading localization file...";

            try
            {
                // Load JSON
                var jsonContent = await File.ReadAllTextAsync(sourceFile);
                var jsonObject = JObject.Parse(jsonContent);

                StatusTextBlock.Text = "🔄 Translating... This may take a while.";

                // Get all keys that need translation (skip special keys starting with __)
                var keysToTranslate = jsonObject.Properties()
                    .Where(p => !p.Name.StartsWith("__"))
                    .ToList();

                var totalKeys = keysToTranslate.Count;
                var translatedCount = 0;

                // Translate each value
                foreach (var property in keysToTranslate)
                {
                    try
                    {
                        var originalText = property.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(originalText))
                        {
                            var translatedText = await _translateService.TranslateAsync(originalText, "en", selectedLanguage);
                            property.Value = translatedText;
                            
                            // Small delay to avoid rate limiting
                            await Task.Delay(100);
                        }

                        translatedCount++;
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Maximum = totalKeys;
                            ProgressBar.Value = translatedCount;
                            StatusTextBlock.Text = $"🔄 Translating... {translatedCount} / {totalKeys} ({Math.Round(translatedCount * 100.0 / totalKeys, 1)}%)";
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        System.Diagnostics.Debug.WriteLine($"Error translating key {property.Name}: {ex.Message}");
                        // Keep original text if translation fails
                        translatedCount++;
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Maximum = totalKeys;
                            ProgressBar.Value = translatedCount;
                            StatusTextBlock.Text = $"⚠️ Translating... {translatedCount} / {totalKeys} (Error on: {property.Name})";
                        });
                    }
                }

                // Update language code in JSON
                if (jsonObject["__LANG"] != null)
                {
                    jsonObject["__LANG"] = selectedLanguage.ToUpper();
                }

                // Save translated JSON
                StatusTextBlock.Text = "💾 Saving translated file...";
                var outputJson = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(targetFile, outputJson);

                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Text = $"✅ Translation completed! {translatedCount} keys translated successfully.";
                    MessageBox.Show($"Translation completed successfully!\n\n{translatedCount} keys translated.\n\nFile saved to:\n{targetFile}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    TranslateButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    StatusTextBlock.Text = $"❌ Error: {ex.Message}";
                    MessageBox.Show($"Error during translation: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TranslateButton.IsEnabled = true;
                });
            }
        }
    }
}

