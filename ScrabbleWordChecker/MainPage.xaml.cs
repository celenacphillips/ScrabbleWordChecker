using Microsoft.Extensions.Configuration;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace ScrabbleWordChecker;

public partial class MainPage : ContentPage
{
    private IConfiguration configuration;
    private static string _baseURI;
    private static string _dictionary;
    private static string _apiKey;

    public MainPage(IConfiguration config)
	{
        InitializeComponent();
        configuration = config;
        GetConfigValues();

        TextInfo ti = new CultureInfo("en-US", false).TextInfo;

        dictionary.Text = $"This app uses Merriam-Webster's {ti.ToTitleCase(_dictionary)}® Dictionary.";
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, EventArgs e)
    {
        wordEntry.Focus();
    }

    private void OnButtonClicked(object sender, EventArgs e)
	{
        if (!string.IsNullOrEmpty(wordEntry.Text))
        {
            string word = wordEntry.Text;

            ResetOutput();
            
            CheckWord.SetSettings(_baseURI, _dictionary, _apiKey);
            List<DictionaryResponse> dictionaryResponse = CheckWord.GetWord(word);
            Word dictionaryWord = CheckWord.CheckWordValidity(dictionaryResponse, word, true);

            ConfigureOutput(dictionaryWord);
        }
        else
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            string text = "Please enter a word!";
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 14;

            var toast = Toast.Make(text, duration, fontSize);

            toast.Show(cancellationTokenSource.Token);
        }
        
    }

    private void Action ()
    {

    }

	private void GetConfigValues()
	{
        Settings settings = configuration.GetRequiredSection("Settings").Get<Settings>();

        _baseURI = settings.BaseURI;
        _dictionary = settings.Dictionary;
        _apiKey = settings.APIKey;
    }

    private class Settings
    {
        public string BaseURI { get; set; }
        public string Dictionary { get; set; }
        public string APIKey { get; set; }
    }

    private void ConfigureOutput(Word dictionaryWord)
    {
        var wordIsValid = false;

        foreach (var valid in dictionaryWord.IsValid)
        {
            if (valid)
                wordIsValid = true;
        }

        if (wordIsValid)
            enteredWord.Text = $"{dictionaryWord.WordName} is valid";
        else
            enteredWord.Text = $"{dictionaryWord.WordName} is NOT valid";

        foreach (var def in dictionaryWord.Definition)
        {
            definition.Text += $"{def}\n";
        }

        if (string.IsNullOrEmpty(definition.Text))
            definition.Text = "There are no definitions";
    }

    private void ResetOutput ()
    {
        //hide keyboard
        wordEntry.IsEnabled = false;
        wordEntry.IsEnabled = true;


        wordEntry.Text = string.Empty;
        definition.Text = string.Empty;
    }
}

