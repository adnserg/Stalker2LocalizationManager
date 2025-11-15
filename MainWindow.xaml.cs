using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Stalker2LocalizationManager
{
    public partial class MainWindow : Window
    {
        private ITranslationService? _translateService;
        private CancellationTokenSource? _cancellationTokenSource;
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            // Load config - ensure it's never null
            _config = AppConfig.Load() ?? new AppConfig();
            // Initialize with free provider by default after window is loaded
            this.Loaded += MainWindow_Loaded;
            // Save config when closing application
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save config before closing
            SaveConfig();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait a bit to ensure all XAML elements are fully loaded before loading config
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded - Starting config load");
                System.Diagnostics.Debug.WriteLine($"Config before load - SourceFile: {_config?.SourceFile}, TargetFile: {_config?.TargetFile}");
                
                // Load saved configuration
                LoadConfig();
                
                System.Diagnostics.Debug.WriteLine($"After LoadConfig - SourceFileTextBox.Text: {SourceFileTextBox?.Text}, TargetFileTextBox.Text: {TargetFileTextBox?.Text}");
                
                // Then initialize provider connection test
                if (LibreTranslateRadio != null && LibreTranslateRadio.IsChecked == true)
                {
                    InitializeProviderConnection("LibreTranslate");
                }
                else if (MyMemoryRadio != null && MyMemoryRadio.IsChecked == true)
                {
                    InitializeProviderConnection("MyMemory");
                }
                else if (GoogleProviderRadio != null && GoogleProviderRadio.IsChecked == true)
                {
                    // For Google, just show the panel, don't test
                    if (GoogleApiKeyPanel != null)
                        GoogleApiKeyPanel.Visibility = Visibility.Visible;
                    if (FreeProviderInfo != null)
                        FreeProviderInfo.Visibility = Visibility.Collapsed;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void InitializeProviderConnection(string provider)
        {
            if (GoogleApiKeyPanel == null || FreeProviderInfo == null || StatusTextBlock == null || FreeProviderInfoText == null)
                return;

            if (provider == "LibreTranslate")
            {
                GoogleApiKeyPanel.Visibility = Visibility.Collapsed;
                FreeProviderInfo.Visibility = Visibility.Visible;
                FreeProviderInfoText.Text = "✅ LibreTranslate is completely free and open-source.\nNo API key required. Uses public translation servers.";
                StatusTextBlock.Text = "🔄 Testing LibreTranslate connection...";
                
                Task.Run(async () =>
                {
                    try
                    {
                        var libreService = new LibreTranslateService();
                        var libreResult = await libreService.TestConnectionAsync();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            if (libreResult)
                            {
                                _translateService = libreService;
                                StatusTextBlock.Text = "✅ LibreTranslate ready! No API key required.";
                            }
                            else
                            {
                                StatusTextBlock.Text = "⚠️ LibreTranslate connection failed. Please check your internet connection or try MyMemory.";
                                _translateService = null;
                            }
                            UpdateTranslateButtonState();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            StatusTextBlock.Text = $"⚠️ LibreTranslate error: {ex.Message}";
                            _translateService = null;
                            UpdateTranslateButtonState();
                        });
                    }
                });
            }
            else if (provider == "MyMemory")
            {
                GoogleApiKeyPanel.Visibility = Visibility.Collapsed;
                FreeProviderInfo.Visibility = Visibility.Visible;
                FreeProviderInfoText.Text = "✅ MyMemory Translation is completely free.\nNo API key required. Uses public translation API.";
                StatusTextBlock.Text = "🔄 Testing MyMemory connection...";
                
                Task.Run(async () =>
                {
                    try
                    {
                        var myMemoryService = new MyMemoryTranslateService();
                        var myMemoryResult = await myMemoryService.TestConnectionAsync();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            if (myMemoryResult)
                            {
                                _translateService = myMemoryService;
                                StatusTextBlock.Text = "✅ MyMemory Translation ready! No API key required.";
                            }
                            else
                            {
                                StatusTextBlock.Text = "⚠️ MyMemory connection failed. Please check your internet connection or try LibreTranslate.";
                                _translateService = null;
                            }
                            UpdateTranslateButtonState();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            StatusTextBlock.Text = $"⚠️ MyMemory error: {ex.Message}";
                            _translateService = null;
                            UpdateTranslateButtonState();
                        });
                    }
                });
            }
        }

        private void LoadConfig()
        {
            try
            {
                // Ensure config is initialized
                if (_config == null)
                {
                    _config = new AppConfig();
                }

                // Check if UI elements are ready
                if (SourceFileTextBox == null || TargetFileTextBox == null || 
                    LanguageComboBox == null || LibreTranslateRadio == null || 
                    MyMemoryRadio == null || GoogleProviderRadio == null)
                {
                    return;
                }

                // Load source file (always load path from config)
                if (!string.IsNullOrEmpty(_config.SourceFile))
                {
                    SourceFileTextBox.Text = _config.SourceFile;
                    System.Diagnostics.Debug.WriteLine($"Loaded source file from config: {_config.SourceFile}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No source file in config");
                }

                // Load target file (always load path, even if file doesn't exist yet)
                if (!string.IsNullOrEmpty(_config.TargetFile))
                {
                    TargetFileTextBox.Text = _config.TargetFile;
                }

                // Load selected language
                if (!string.IsNullOrEmpty(_config.SelectedLanguage))
                {
                    foreach (ComboBoxItem item in LanguageComboBox.Items)
                    {
                        if (item.Tag?.ToString() == _config.SelectedLanguage)
                        {
                            LanguageComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Load selected provider (set without triggering events - SaveConfig is only called on close)
                if (!string.IsNullOrEmpty(_config.SelectedProvider))
                {
                    switch (_config.SelectedProvider)
                    {
                        case "LibreTranslate":
                            if (LibreTranslateRadio != null)
                                LibreTranslateRadio.IsChecked = true;
                            break;
                        case "MyMemory":
                            if (MyMemoryRadio != null)
                                MyMemoryRadio.IsChecked = true;
                            break;
                        case "Google":
                            if (GoogleProviderRadio != null)
                                GoogleProviderRadio.IsChecked = true;
                            break;
                    }
                }
                else
                {
                    // Default to LibreTranslate if no provider saved
                    if (LibreTranslateRadio != null)
                        LibreTranslateRadio.IsChecked = true;
                }

                UpdateTranslateButtonState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                // Ensure config is initialized even on error
                if (_config == null)
                {
                    _config = new AppConfig();
                }
            }
        }

        private void SaveConfig()
        {
            try
            {
                // Ensure config is initialized
                if (_config == null)
                {
                    _config = new AppConfig();
                }

                // Check if UI elements are ready
                if (SourceFileTextBox == null || TargetFileTextBox == null || 
                    LanguageComboBox == null || LibreTranslateRadio == null || 
                    MyMemoryRadio == null || GoogleProviderRadio == null)
                {
                    System.Diagnostics.Debug.WriteLine("UI elements not ready, skipping config save");
                    return;
                }

                _config.SourceFile = SourceFileTextBox.Text;
                _config.TargetFile = TargetFileTextBox.Text;
                _config.SelectedLanguage = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                
                if (LibreTranslateRadio.IsChecked == true)
                    _config.SelectedProvider = "LibreTranslate";
                else if (MyMemoryRadio.IsChecked == true)
                    _config.SelectedProvider = "MyMemory";
                else if (GoogleProviderRadio.IsChecked == true)
                    _config.SelectedProvider = "Google";

                _config.Save();
                System.Diagnostics.Debug.WriteLine($"Config saved - SourceFile: {_config.SourceFile}, TargetFile: {_config.TargetFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Source Localization File"
            };

            // Set initial directory from config if available
            if (_config != null && !string.IsNullOrEmpty(_config.SourceFile) && File.Exists(_config.SourceFile))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(_config.SourceFile);
                dialog.FileName = Path.GetFileName(_config.SourceFile);
            }

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

            // Set initial directory from config if available
            if (_config != null && !string.IsNullOrEmpty(_config.TargetFile))
            {
                var targetDir = Path.GetDirectoryName(_config.TargetFile);
                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    dialog.InitialDirectory = targetDir;
                }
                dialog.FileName = Path.GetFileName(_config.TargetFile);
            }

            if (dialog.ShowDialog() == true)
            {
                TargetFileTextBox.Text = dialog.FileName;
                UpdateTranslateButtonState();
            }
        }

        private void ProviderRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Check if elements are initialized
            if (GoogleApiKeyPanel == null || FreeProviderInfo == null || StatusTextBlock == null || FreeProviderInfoText == null)
                return;

            // Don't save config here - will be saved on application close

            if (LibreTranslateRadio.IsChecked == true)
            {
                // LibreTranslate provider
                GoogleApiKeyPanel.Visibility = Visibility.Collapsed;
                FreeProviderInfo.Visibility = Visibility.Visible;
                FreeProviderInfoText.Text = "✅ LibreTranslate is completely free and open-source.\nNo API key required. Uses public translation servers.";
                StatusTextBlock.Text = "🔄 Testing LibreTranslate connection...";
                
                // Test connection automatically
                Task.Run(async () =>
                {
                    try
                    {
                        var libreService = new LibreTranslateService();
                        var libreResult = await libreService.TestConnectionAsync();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            if (libreResult)
                            {
                                _translateService = libreService;
                                StatusTextBlock.Text = "✅ LibreTranslate ready! No API key required.";
                            }
                            else
                            {
                                StatusTextBlock.Text = "⚠️ LibreTranslate connection failed. Please check your internet connection or try MyMemory.";
                                _translateService = null;
                            }
                            UpdateTranslateButtonState();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            StatusTextBlock.Text = $"⚠️ LibreTranslate error: {ex.Message}";
                            _translateService = null;
                            UpdateTranslateButtonState();
                        });
                    }
                });
            }
            else if (MyMemoryRadio.IsChecked == true)
            {
                // MyMemory provider
                GoogleApiKeyPanel.Visibility = Visibility.Collapsed;
                FreeProviderInfo.Visibility = Visibility.Visible;
                FreeProviderInfoText.Text = "✅ MyMemory Translation is completely free.\nNo API key required. Uses public translation API.";
                StatusTextBlock.Text = "🔄 Testing MyMemory connection...";
                
                // Test connection automatically
                Task.Run(async () =>
                {
                    try
                    {
                        var myMemoryService = new MyMemoryTranslateService();
                        var myMemoryResult = await myMemoryService.TestConnectionAsync();
                        
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            if (myMemoryResult)
                            {
                                _translateService = myMemoryService;
                                StatusTextBlock.Text = "✅ MyMemory Translation ready! No API key required.";
                            }
                            else
                            {
                                StatusTextBlock.Text = "⚠️ MyMemory connection failed. Please check your internet connection or try LibreTranslate.";
                                _translateService = null;
                            }
                            UpdateTranslateButtonState();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (StatusTextBlock == null) return;
                            
                            StatusTextBlock.Text = $"⚠️ MyMemory error: {ex.Message}";
                            _translateService = null;
                            UpdateTranslateButtonState();
                        });
                    }
                });
            }
            else if (GoogleProviderRadio.IsChecked == true)
            {
                // Google provider
                GoogleApiKeyPanel.Visibility = Visibility.Visible;
                FreeProviderInfo.Visibility = Visibility.Collapsed;
                _translateService = null; // Reset until API key is tested
                StatusTextBlock.Text = "Please enter Google Translate API key and test connection.";
                UpdateTranslateButtonState();
            }
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateTranslateButtonState();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Don't save config here - will be saved on application close
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
                            _translateService = null;
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
                        _translateService = null;
                        UpdateTranslateButtonState();
                    });
                }
            });
        }

        private void UpdateTranslateButtonState()
        {
            // Check if UI elements are initialized
            if (TranslateButton == null || SourceFileTextBox == null || TargetFileTextBox == null)
                return;

            var hasSourceFile = !string.IsNullOrWhiteSpace(SourceFileTextBox.Text) && File.Exists(SourceFileTextBox.Text);
            var hasTargetFile = !string.IsNullOrWhiteSpace(TargetFileTextBox.Text);
            
            if (LibreTranslateRadio != null && MyMemoryRadio != null && 
                (LibreTranslateRadio.IsChecked == true || MyMemoryRadio.IsChecked == true))
            {
                // Free providers - no API key needed
                TranslateButton.IsEnabled = hasSourceFile && hasTargetFile && _translateService != null;
            }
            else if (GoogleProviderRadio != null && GoogleProviderRadio.IsChecked == true)
            {
                // Google provider - API key and test required
                var hasApiKey = ApiKeyBox != null && !string.IsNullOrWhiteSpace(ApiKeyBox.Password);
                TranslateButton.IsEnabled = hasSourceFile && 
                                           hasTargetFile && 
                                           hasApiKey &&
                                           _translateService != null;
            }
            else
            {
                TranslateButton.IsEnabled = false;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_translateService == null)
            {
                if (GoogleProviderRadio.IsChecked == true)
                {
                    MessageBox.Show("Please test API connection first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Translation service is not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            // Check if target file exists and ask for confirmation BEFORE starting translation
            if (File.Exists(targetFile))
            {
                var result = MessageBox.Show(
                    $"File already exists:\n{targetFile}\n\nDo you want to overwrite it?",
                    "File Exists",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Create cancellation token
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            // Update UI
            TranslateButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StopButton.Visibility = Visibility.Visible;
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
                var wasCancelled = false;

                // Translate each value
                foreach (var property in keysToTranslate)
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        break;
                    }

                    try
                    {
                        var originalText = property.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(originalText))
                        {
                            var translatedText = await _translateService.TranslateAsync(originalText, "en", selectedLanguage);
                            property.Value = translatedText;
                            
                            // Small delay to avoid rate limiting
                            await Task.Delay(100, cancellationToken);
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
                    catch (OperationCanceledException)
                    {
                        wasCancelled = true;
                        break;
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

                // Save translated JSON (even if cancelled, save what we have)
                StatusTextBlock.Text = wasCancelled ? "⏹ Saving partial translation..." : "💾 Saving translated file...";
                var outputJson = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(targetFile, outputJson);
                
                // Config will be saved on application close

                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Collapsed;
                    StopButton.IsEnabled = false;
                    
                    if (wasCancelled)
                    {
                        StatusTextBlock.Text = $"⏹ Translation stopped. {translatedCount} / {totalKeys} keys translated and saved.";
                        MessageBox.Show($"Translation stopped by user.\n\n{translatedCount} out of {totalKeys} keys translated.\n\nPartial translation saved to:\n{targetFile}", 
                            "Translation Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusTextBlock.Text = $"✅ Translation completed! {translatedCount} keys translated successfully.";
                        MessageBox.Show($"Translation completed successfully!\n\n{translatedCount} keys translated.\n\nFile saved to:\n{targetFile}", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                    TranslateButton.IsEnabled = true;
                });
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation during file operations
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Collapsed;
                    StopButton.IsEnabled = false;
                    StatusTextBlock.Text = "⏹ Translation stopped.";
                    TranslateButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Collapsed;
                    StopButton.IsEnabled = false;
                    StatusTextBlock.Text = $"❌ Error: {ex.Message}";
                    MessageBox.Show($"Error during translation: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TranslateButton.IsEnabled = true;
                });
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}

