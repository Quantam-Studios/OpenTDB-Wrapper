using OpenTDB.Exceptions;
using OpenTDB.Models;
using OpenTDB.Enumerators;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Encoding = OpenTDB.Enumerators.Encoding;

namespace OpenTDB
{
    public class OpenTDB
    {
        private readonly HttpClient HttpClient;
        private Token? Token = null;

        // Constructor with optional HttpClient and token parameters
        public OpenTDB(HttpClient? httpClient = null)
        {
            // If an HttpClient is provided, use it; otherwise, create a new instance
            HttpClient = httpClient ?? new();
        }

        /// <summary>
        /// Initializes the session token asynchronously if it has not been initialized already.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// This method initializes the session token obtained from the Open Trivia Database (OpenTDB) API. 
        /// If the token has already been initialized, this method does nothing. 
        /// To ensure that the token is initialized before making API requests, 
        /// it is recommended to call this method after creating an instance of <see cref="OpenTDB"/>.
        /// </remarks>
        public async Task InitializeTokenAsync()
        {
            if (Token == null)
            {
                Token = await RequestTokenAsync();
            }
        }

        /// <summary>
        /// Asynchronously resets the session token obtained from the Open Trivia Database.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="OpenTDBException">
        /// Thrown if the token has not been set previously or if an error occurs during the HTTP request or JSON parsing.
        /// </exception>
        public async Task ResetTokenAsync()
        {
            if (Token == null)
            {
                throw new OpenTDBException("You cannot reset a token that was never set.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://opentdb.com/api_token.php?command=reset&token={Token.Value}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                // Check if the response is null, indicating token deletion
                if (response == null)
                {
                    Token = await RequestTokenAsync();
                    return;
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                Token = ParseTokenResponse(content);
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Requests a session token from the Open Trivia Database.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains
        /// a <see cref="Token"/> object representing the session token obtained from the API.
        /// </returns>
        /// <exception cref="OpenTDBException">
        /// Thrown when an error occurs during the HTTP request or JSON parsing.
        /// </exception>
        private async Task<Token> RequestTokenAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://opentdb.com/api_token.php?command=request");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return ParseTokenResponse(content);
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Requests trivia questions from the Open Trivia Database with a specified encoding.
        /// </summary>
        /// <param name="questionCount">Number of questions to request (1 to 50).</param>
        /// <param name="category">Category of the questions.</param>
        /// <param name="difficulty">Difficulty level of the questions.</param>
        /// <param name="type">Type of questions (MultipleChoice or TrueFalse).</param>
        /// <param name="encoding">Encoding for the questions.</param>
        /// <returns>List of trivia questions.</returns>
        /// <exception cref="OpenTDBException">Thrown if the HTTP request fails or JSON parsing fails.</exception>
        /// <exception cref="ArgumentException">Thrown if questionCount is 0 or greater than 50.</exception>
        public async Task<List<Question>> GetQuestionsWithEncodingAsync(uint questionCount, Category category = Category.Any, Difficulty difficulty = Difficulty.Any, QuestionType type = QuestionType.Any, Encoding encoding = Encoding.HTML)
        {
            // Check if questionCount is within the allowed range
            if (questionCount == 0 || questionCount > 50)
            {
                throw new ArgumentException("Question count must be between 1 and 50 (inclusive).", nameof(questionCount));
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, CreateLink(questionCount, category, difficulty, type, encoding));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return ParseQuestionResponse(content);
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Requests trivia questions from the Open Trivia Database and returns them in plain text.
        /// </summary>
        /// <param name="questionCount">Number of questions to request (1 to 50).</param>
        /// <param name="category">Category of the questions.</param>
        /// <param name="difficulty">Difficulty level of the questions.</param>
        /// <param name="type">Type of questions (MultipleChoice or TrueFalse).</param>
        /// <returns>List of trivia questions.</returns>
        /// <exception cref="OpenTDBException">Thrown if the HTTP request fails or JSON parsing fails.</exception>
        /// <exception cref="ArgumentException">Thrown if questionCount is 0 or greater than 50.</exception>
        public async Task<List<Question>> GetQuestionsAsync(uint questionCount, Category category = Category.Any, Difficulty difficulty = Difficulty.Any, QuestionType type = QuestionType.Any)
        {
            // Check if questionCount is within the allowed range
            if (questionCount == 0 || questionCount > 50)
            {
                throw new ArgumentException("Question count must be between 1 and 50 (inclusive).", nameof(questionCount));
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, CreateLink(questionCount, category, difficulty, type, Encoding.Base64));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = ParseQuestionResponse(content);
                ConvertBase64Question(ref parsedResponse);
                return parsedResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the total count of questions for a specific category.
        /// </summary>
        /// <param name="category">Category for which to retrieve question totals.</param>
        /// <returns>Object containing category question counts.</returns>
        /// <exception cref="OpenTDBException">Thrown if the HTTP request fails or JSON parsing fails.</exception>
        public async Task<CategoryCount> GetCategoryQuestionTotalsAsync(Category category)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://opentdb.com/api_count.php?category={GetCategoryId(category)}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = ParseCategoryResponse(content);
                return parsedResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the total count of questions for a specific category using the category ID.
        /// </summary>
        /// <param name="categoryId">ID of the category for which to retrieve question totals.</param>
        /// <returns>Object containing category question counts.</returns>
        /// <exception cref="OpenTDBException">Thrown if the HTTP request fails or JSON parsing fails.</exception>
        /// <exception cref="ArgumentException">Thrown if categoryId is not within the range of 9 and 32 (inclusive).</exception>
        public async Task<CategoryCount> GetCategoryQuestionTotalsAsync(uint categoryId)
        {
            // Check if categoryId is within the allowed range
            if (categoryId < 9 | categoryId > 32)
            {
                throw new ArgumentException("Cateogry ID must be between 9 and 32 (inclusive).", nameof(categoryId));
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://opentdb.com/api_count.php?category={categoryId}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = ParseCategoryResponse(content);
                return parsedResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves global question totals from the Open Trivia Database (OpenTDB) API.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// The task result contains a <see cref="GlobalCount"/> object representing global question counts.</returns>
        /// <exception cref="OpenTDBException">Thrown when the HTTP request fails or JSON parsing encounters an error.</exception>
        public async Task<GlobalCount> GetGlobalQuestionTotalsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://opentdb.com/api_count_global.php");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsedResponse = ParseGlobalTotalResponse(content);
                return parsedResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new OpenTDBException($"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new OpenTDBException($"JSON parsing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates the API link based on provided parameters.
        /// </summary>
        /// <param name="questionCount">Number of questions to include in the request.</param>
        /// <param name="category">Category of the questions.</param>
        /// <param name="difficulty">Difficulty level of the questions.</param>
        /// <param name="type">Type of the questions (multiple choice or true/false).</param>
        /// <param name="encoding">Encoding type for the questions.</param>
        /// <returns>API link as a string.</returns>
        public string CreateLink(uint questionCount, Category category = Category.Any, Difficulty difficulty = Difficulty.Any, QuestionType type = QuestionType.Any, Encoding encoding = Encoding.HTML)
        {
            StringBuilder link = new();
            link.Append("https://opentdb.com/api.php?");

            if (questionCount > 0)
            {
                link.Append($"amount={questionCount}");
            }

            if (category != Category.Any)
            {
                link.Append(GetCategoryString(category));
            }

            if (difficulty != Difficulty.Any)
            {
                link.Append(GetDifficultyString(difficulty));
            }

            if (type != QuestionType.Any)
            {
                link.Append(GetQuestionTypeString(type));
            }

            if (encoding != Encoding.HTML)
            {
                link.Append(GetEncodingString(encoding));
            }

            if (Token != null)
            {
                link.Append($"&token={Token.Value}");
            }

            return link.ToString();
        }

        /// <summary>
        /// Parses the response received from the Open Trivia Database after requesting a session token.
        /// </summary>
        /// <param name="response">The JSON response received from the API.</param>
        /// <returns>A <see cref="Token"/> object representing the session token obtained from the API.</returns>
        /// <exception cref="OpenTDBException">
        /// Thrown when the response format is invalid or contains an error code indicating a failure in token acquisition.
        /// </exception>
        private static Token ParseTokenResponse(string response)
        {
            Token token = new();

            var jsonData = JsonNode.Parse(response).AsObject();

            // Response Codes
            if (jsonData.TryGetPropertyValue("response_code", out var responseCodeNode) && responseCodeNode is JsonValue responseCode)
            {
                var code = (int)responseCode;
                if (code != 0)
                {
                    throw new OpenTDBException(GetResponseMessage(code));
                }
            }
            else
            {
                throw new OpenTDBException("Invalid response format. Response code is missing.");
            }

            // Token Value
            token.Value = jsonData["token"].ToString();

            return token;
        }

        /// <summary>
        /// Parses the API response and returns a list of questions.
        /// </summary>
        /// <param name="response">API response as a string.</param>
        /// <returns>List of parsed questions.</returns>
        private static List<Question> ParseQuestionResponse(string response)
        {
            var jsonData = JsonNode.Parse(response).AsObject();

            // Response Codes
            if (jsonData.TryGetPropertyValue("response_code", out var responseCodeNode) && responseCodeNode is JsonValue responseCode)
            {
                var code = (int)responseCode;
                if (code != 0)
                {
                    throw new OpenTDBException(GetResponseMessage(code));
                }
            }
            else
            {
                throw new OpenTDBException("Invalid response format. Response code is missing.");
            }

            // Parse Questions
            var results = jsonData["results"].AsArray();

            List<Question> questions = new();

            foreach (var result in results)
            {
                Question question = new()
                {
                    QuestionTitle = result["question"].ToString(),
                    Difficulty = result["difficulty"].ToString(),
                    Category = result["category"].ToString(),
                    IncorrectAnswers = result["incorrect_answers"].AsArray().Select(e => e.ToString()).ToArray(),
                    CorrectAnswer = result["correct_answer"].ToString()
                };

                questions.Add(question);
            }

            return questions;
        }

        /// <summary>
        /// Parses the API response for category question totals and returns a CategoryCount object.
        /// </summary>
        /// <param name="response">API response as a string.</param>
        /// <returns>Object containing category question counts.</returns>
        private static CategoryCount ParseCategoryResponse(string response)
        {
            CategoryCount categoryCount = new();

            var jsonData = JsonNode.Parse(response).AsObject();
    
            // Category ID
            categoryCount.CategoryId = Int16.Parse(jsonData["category_id"].ToString());

            var jsonCategoryCount = JsonNode.Parse(jsonData["category_question_count"].ToString()).AsObject();

            // Category Totals
            categoryCount.TotalQuestions = Int16.Parse(jsonCategoryCount["total_question_count"].ToString());
            categoryCount.TotalEasyQuestions = Int16.Parse(jsonCategoryCount["total_easy_question_count"].ToString());
            categoryCount.TotalMediumQuestions = Int16.Parse(jsonCategoryCount["total_medium_question_count"].ToString());
            categoryCount.TotalHardQuestions = Int16.Parse(jsonCategoryCount["total_hard_question_count"].ToString());
            
            return categoryCount;
        }

        /// <summary>
        /// Parses the global question totals from the JSON response received from the Open Trivia Database (OpenTDB) API.
        /// </summary>
        /// <param name="response">The JSON response string containing global question totals.</param>
        /// <returns>A <see cref="GlobalCount"/> object representing global question counts.</returns>
        private static GlobalCount ParseGlobalTotalResponse(string response)
        {
            GlobalCount globalCount = new();

            var jsonData = JsonNode.Parse(response).AsObject();
            var jsonTotalCounts = JsonNode.Parse(jsonData["overall"].ToString()).AsObject();

            // Overall
            globalCount.TotalQuestions = Int32.Parse(jsonTotalCounts["total_num_of_questions"].ToString());
            globalCount.TotalVerifiedQuestions = Int32.Parse(jsonTotalCounts["total_num_of_verified_questions"].ToString());
            globalCount.TotalPendingQuestions = Int32.Parse(jsonTotalCounts["total_num_of_pending_questions"].ToString());
            globalCount.TotalRejectedQuestions = Int32.Parse(jsonTotalCounts["total_num_of_rejected_questions"].ToString());

            var jsonCategories = jsonData["categories"].AsObject();
            globalCount.Categories = new();

            // Categories
            foreach (var categoryNode in jsonCategories)
            {
                var categoryId = int.Parse(categoryNode.Key);
                var categoryData = categoryNode.Value.AsObject();

                var categoryTotals = new GlobalCategoryCount
                {
                    CategoryId = categoryId,
                    TotalQuestions = Int32.Parse(categoryData["total_num_of_questions"].ToString()),
                    PendingQuestions = Int32.Parse(categoryData["total_num_of_pending_questions"].ToString()),
                    VerifiedQuestions = Int32.Parse(categoryData["total_num_of_verified_questions"].ToString()),
                    RejectedQuestions = Int32.Parse(categoryData["total_num_of_rejected_questions"].ToString())
                };

                globalCount.Categories.Add(categoryTotals);
            }

            return globalCount;
        }

        /// <summary>
        /// Converts base64-encoded strings in the list of questions to their original UTF-8 representations.
        /// </summary>
        /// <param name="questions">List of questions to be modified.</param>
        private static void ConvertBase64Question(ref List<Question> questions)
        {
            foreach (var question in questions)
            {
                question.Category = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.Category));
                question.Difficulty = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.Difficulty));
                question.QuestionTitle = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.QuestionTitle));
                question.Type = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.Type));
                question.CorrectAnswer = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.CorrectAnswer));
                
                for (int i = 0; i < question.IncorrectAnswers.Length; i++)
                {
                    question.IncorrectAnswers[i] = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(question.IncorrectAnswers[i]));
                }
            }
        }

        /// <summary>
        /// Gets the response message based on the response code.
        /// </summary>
        /// <param name="code">Response code.</param>
        /// <returns>Response message.</returns>
        private static string GetResponseMessage(int code)
        {
            switch (code)
            {
                case 0:
                    return "Code 0: Success | Returned results successfully.";
                case 1:
                    return "Code 1: No Results | Could not return results. The API doesn't have enough questions for your query. (Ex. Asking for 50 Questions in a Category that only has 20.)";
                case 2:
                    return "Code 2: Invalid | Parameter Contains an invalid parameter. Arguements passed in aren't valid. (Ex. Amount = Five)";
                case 3:
                    return "Code 3: Token Not Found | Session Token does not exist.";
                case 4:
                    return "Code 4: Token Empty | Session Token has returned all possible questions for the specified query. Resetting the Token is necessary.";
                case 5:
                    return "Code 5: Rate Limit | Too many requests have occurred. Each IP can only access the API once every 5 seconds.";
                default:
                    return $"Invalid response code: {code}. (If you are seeing this, something unknown happened).";
            }
        }

        /// <summary>
        /// Gets the category string based on the provided category.
        /// </summary>
        /// <param name="category">Category enum value.</param>
        /// <returns>Category string for the API link.</returns>
        private static string GetCategoryString(Category category) 
        {
            int categoryValue = GetCategoryId(category);

            return $"&category={categoryValue}";
        }

        /// <summary>
        /// Gets the category ID based on the provided category.
        /// </summary>
        /// <param name="category">Category enum value.</param>
        /// <returns>Category ID as an integer. 0 if Category.Any</returns>
        public static int GetCategoryId(Category category)
        {
            switch (category)
            {
                case Category.Any:
                    return 0;
                case Category.GeneralKnowledge:
                    return 9;
                case Category.Books:
                    return 10;
                case Category.Film:
                    return 11;
                case Category.Music:
                    return 12;
                case Category.MusicalsTheatres:
                    return 13;
                case Category.Television:
                    return 14;
                case Category.VideoGames:
                    return 15;
                case Category.BoardGames:
                    return 16;
                case Category.Nature:
                    return 17;
                case Category.Computers:
                    return 18;
                case Category.Mathematics:
                    return 19;
                case Category.Mythology:
                    return 20;
                case Category.Sports:
                    return 21;
                case Category.Geography:
                    return 22;
                case Category.History:
                    return 23;
                case Category.Politics:
                    return 24;
                case Category.Art:
                    return 25;
                case Category.Celebrities:
                    return 26;
                case Category.Animals:
                    return 27;
                case Category.Vehicles:
                    return 28;
                case Category.Comics:
                    return 29;
                case Category.Gadgets:
                    return 30;
                case Category.AnimeManga:
                    return 31;
                case Category.CartoonsAnimations:
                    return 32;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the difficulty string based on the provided difficulty.
        /// </summary>
        /// <param name="difficulty">Difficulty enum value.</param>
        /// <returns>Difficulty string for the API link.</returns>
        private static string GetDifficultyString(Difficulty difficulty)
        {
            string difficultyValue = "";

            switch (difficulty) 
            {
                case Difficulty.Any:
                    return "";
                case Difficulty.Easy:
                    difficultyValue = "easy";
                    break;
                case Difficulty.Medium:
                    difficultyValue = "medium";
                    break;
                case Difficulty.Hard:
                    difficultyValue = "hard";
                    break;
            }

            return $"&difficulty={difficultyValue}";
        }

        /// <summary>
        /// Gets the question type string based on the provided question type.
        /// </summary>
        /// <param name="type">Question type enum value.</param>
        /// <returns>Question type string for the API link.</returns>
        private static string GetQuestionTypeString(QuestionType type)
        {
            string typeValue = "";
            
            switch (type) 
            {
                case QuestionType.Any:
                    return "";
                case QuestionType.MultipleChoice:
                    typeValue = "multiple";
                    break;
                case QuestionType.TrueFalse:
                    typeValue = "boolean";
                    break;
            }

            return $"&type={typeValue}";
        }

        /// <summary>
        /// Gets the encoding string based on the provided encoding.
        /// </summary>
        /// <param name="encoding">Encoding enum value.</param>
        /// <returns>Encoding string for the API link.</returns>
        private static string GetEncodingString(Encoding encoding) 
        {
            string encodingValue = "";

            switch (encoding) 
            {
                case Encoding.HTML:
                    return "";
                case Encoding.LegacyURL:
                    encodingValue = "urlLegacy";
                    break;
                case Encoding.URL:
                    encodingValue = "url3986";
                    break;
                case Encoding.Base64:
                    encodingValue = "base64";
                    break;
            }

            return $"&encode={encodingValue}";
        }
    }
}