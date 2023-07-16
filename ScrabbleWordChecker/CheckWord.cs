
using Newtonsoft.Json;

namespace ScrabbleWordChecker
{
    public class CheckWord
    {
        private static string _baseURI;
        private static string _dictionary;
        private static string _apiKey;

        public static void SetSettings (string baseURI, string dictionary, string apiKey)
        {
            _baseURI = baseURI;
            _dictionary = dictionary;
            _apiKey = apiKey;
        }

        public static List<DictionaryResponse> GetWord (string word)
        {
            HttpClient client = GetDictionaryClient(_baseURI);
            var endpoint = $"{client.BaseAddress}{_dictionary}/json/{word}?key={_apiKey}";
            var response = client.GetAsync(endpoint).GetAwaiter().GetResult();

            List <DictionaryResponse> dictionaryResponse = new();

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;

                try
                {
                    dictionaryResponse = JsonConvert.DeserializeObject<List<DictionaryResponse>>(result);
                }
                catch
                {
                    dictionaryResponse.Add(
                        new DictionaryResponse()
                        {
                            meta = new Meta()
                            {
                                id = word
                            },
                            fl = null,
                            cxs = new List<Cx>()
                        }
                    );
                }
            }
            else
            {
                throw new Exception($"Error getting word: {endpoint}");
            }

            return dictionaryResponse;
        }

        public static Word CheckWordValidity (List<DictionaryResponse> dictionaryResponse, string word, bool isBaseWord)
        {
            Word dictionaryWord = new()
            {
                WordName = word,
                Definition = new List<string>(),
                IsValid = new List<bool>()
            };

            foreach (var response in dictionaryResponse)
            {
                // Only want exact matches
                // No hypenated words
                if (response.meta.id.ToLower().Equals(word.ToLower()) ||
                    response.meta.id.ToLower().Contains($"{word.ToLower()}:") ||
                    response.meta.id.ToLower().Equals($"{word.ToLower()}-"))
                {
                    if (!string.IsNullOrEmpty(response.fl))
                    {
                        var functionalLabel = response.fl;

                        // Scrabble rules:
                        // No abbreviations
                        // No prefixes
                        // No suffixes
                        if (!functionalLabel.Contains("abbreviation") &&
                            !functionalLabel.Contains("prefix") &&
                            !functionalLabel.Contains("suffix"))
                        {
                            dictionaryWord.IsValid.Add(true);
                        }
                        else
                        {
                            dictionaryWord.IsValid.Add(false);
                        }

                        if (response.shortdef.Any())
                        {
                            foreach (var shortDefinition in response.shortdef)
                            {
                                dictionaryWord.Definition.Add($"\t{functionalLabel}: {shortDefinition}");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(response.fl))
                    {
                        if (response.cxs.Any() && isBaseWord)
                        {
                            foreach (var cognateCrossReference in response.cxs)
                            {
                                foreach (var cognateCrossReferenceTarget in cognateCrossReference.cxtis)
                                {
                                    List<DictionaryResponse> cognateDictionaryReponse = GetWord(cognateCrossReferenceTarget.cxt);

                                    Word cognateWord = CheckWordValidity(cognateDictionaryReponse, cognateCrossReferenceTarget.cxt, false);
                                    foreach (var valid in cognateWord.IsValid)
                                    {
                                        dictionaryWord.IsValid.Add(valid);
                                    }

                                    dictionaryWord.Definition.Add($"\t{cognateCrossReference.cxl} {cognateCrossReferenceTarget.cxt}");
                                    dictionaryWord.Definition.Add($"\n{cognateWord.WordName.ToUpper()}");

                                    foreach (var def in cognateWord.Definition)
                                    {
                                        dictionaryWord.Definition.Add(def);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dictionaryWord;
        }

        private static HttpClient GetDictionaryClient(string baseURI)
        {
            HttpClient client = new();
            client.BaseAddress = new Uri(baseURI);

            return client;
        }
    }
}
