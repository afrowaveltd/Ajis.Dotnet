using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using AjisFile = Afrowave.AJIS.IO.AjisFile;

namespace AjisCountriesTest;

public class Country
{
   public string? Name { get; set; }
   public string? Capital { get; set; }
   public string? Region { get; set; }
}

public class App : Application
{
   public override void Initialize()
   {
      AvaloniaXamlLoader.Load(this);
   }

   public override void OnFrameworkInitializationCompleted()
   {
      if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
         desktop.MainWindow = new MainWindow();
      }
      base.OnFrameworkInitializationCompleted();
   }
}

public class MainWindow : Window
{
   private readonly ObservableCollection<Country> _countries = [];
   private readonly string _ajisFile = Path.Combine(AppContext.BaseDirectory, "countries.ajis");
   private readonly StackPanel? _mainPanel;
   private readonly TextBlock? _statusBlock;
   private bool _isLoaded = false;

   public MainWindow()
   {
      Title = "Ajis Countries Test";
      Width = 800;
      Height = 600;

      _mainPanel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };

      TextBox searchBox = new TextBox { Width = 300, Text = "" };
      searchBox.TextChanged += (s, e) => OnSearchChanged();

      Button loadBtn = new Button { Content = "Load Countries", Width = 120 };
      loadBtn.Click += LoadCountries;

      _statusBlock = new TextBlock { Text = "Ready - click Load button" };

      _mainPanel.Children.Add(searchBox);
      _mainPanel.Children.Add(loadBtn);
      _mainPanel.Children.Add(_statusBlock);

      Content = _mainPanel;
   }

   private void OnSearchChanged()
   {
      UpdateView("");
   }

   private async void LoadCountries(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   {
      await LoadCountriesAsync();
   }

   private async Task LoadCountriesAsync()
   {
      if(_isLoaded)
      {
         UpdateView("");
         return;
      }

      _statusBlock!.Text = "Loading...";

      try
      {
         _countries.Clear();

         var countries = AjisFile.Enumerate<Country>(_ajisFile);
         foreach(var country in countries)
         {
            _countries.Add(country);
         }

         _isLoaded = true;
         _statusBlock.Text = $"Loaded {_countries.Count} countries";
         UpdateView("");
      }
      catch(Exception ex)
      {
         _statusBlock.Text = $"Error: {ex.Message}";
      }
   }

   private void UpdateView(string search)
   {
      if(Content is not StackPanel content || content.Children.Count < 3)
         return;

      for(int i = content.Children.Count - 1; i >= 3; i--)
      {
         content.Children.RemoveAt(i);
      }

      if(_countries.Count == 0)
      {
         content.Children.Add(new TextBlock { Text = "No countries loaded. Click 'Load Countries' button." });
         return;
      }

      var filtered = string.IsNullOrEmpty(search)
          ? [.. _countries]
          : _countries.Where(c =>
              (c.Name?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
              (c.Capital?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
              (c.Region?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)).OrderBy(c => c.Name).ToList();

      content.Children.Add(new TextBlock { Text = $"Showing {filtered.Count} of {_countries.Count} countries" });

      foreach(var country in filtered)
      {
         var text = $"{country.Name ?? ""}";
         if(!string.IsNullOrEmpty(country.Capital))
            text += $" - {country.Capital}";
         if(!string.IsNullOrEmpty(country.Region))
            text += $" - {country.Region}";
         content.Children.Add(new TextBlock { Text = text });
      }
   }
}