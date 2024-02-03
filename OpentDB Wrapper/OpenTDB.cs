﻿using OpenTDB.Exceptions;
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
        private static readonly HttpClient HttpClient = new();

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

                return ParseResponse(content);
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
                var parsedResponse = ParseResponse(content);
                ConvertBase64Response(ref parsedResponse);
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

            return link.ToString();
        }

        /// <summary>
        /// Parses the API response and returns a list of questions.
        /// </summary>
        /// <param name="response">API response as a string.</param>
        /// <returns>List of parsed questions.</returns>
        private List<Question> ParseResponse(string response)
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
        /// Converts base64-encoded strings in the list of questions to their original UTF-8 representations.
        /// </summary>
        /// <param name="questions">List of questions to be modified.</param>
        private void ConvertBase64Response(ref List<Question> questions)
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
        private string GetResponseMessage(int code)
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
        private string GetCategoryString(Category category) 
        {
            int categoryValue = 0;

            switch (category)
            {
                case Category.Any:
                    return "";
                case Category.GeneralKnowledge:
                    categoryValue = 9;
                    break;
                case Category.Books:
                    categoryValue = 10;
                    break;
                case Category.Film:
                    categoryValue = 11;
                    break;
                case Category.Music:
                    categoryValue = 12;
                    break;
                case Category.MusicalsTheatres:
                    categoryValue = 13;
                    break;
                case Category.Television:
                    categoryValue = 14;
                    break;
                case Category.VideoGames:
                    categoryValue = 15;
                    break;
                case Category.BoardGames:
                    categoryValue = 16;
                    break;
                case Category.Nature:
                    categoryValue = 17;
                    break;
                case Category.Computers:
                    categoryValue = 18;
                    break;
                case Category.Mathematics:
                    categoryValue = 19;
                    break;
                case Category.Mythology:
                    categoryValue = 20;
                    break;
                case Category.Sports:
                    categoryValue = 21;
                    break;
                case Category.Geography:
                    categoryValue = 22;
                    break;
                case Category.History:
                    categoryValue = 23;
                    break;
                case Category.Politics:
                    categoryValue = 24;
                    break;
                case Category.Art:
                    categoryValue = 25;
                    break;
                case Category.Celebrities:
                    categoryValue = 26; 
                    break;
                case Category.Animals:
                    categoryValue = 27;
                    break;
                case Category.Vehicles:
                    categoryValue = 28;
                    break;
                case Category.Comics:
                    categoryValue = 29;
                    break;
                case Category.Gadgets:
                    categoryValue = 30;
                    break;
                case Category.AnimeManga:
                    categoryValue = 31;
                    break;
                case Category.CartoonsAnimations:
                    categoryValue = 32;
                    break;
            }

            return $"&category={categoryValue}";
        }

        /// <summary>
        /// Gets the difficulty string based on the provided difficulty.
        /// </summary>
        /// <param name="difficulty">Difficulty enum value.</param>
        /// <returns>Difficulty string for the API link.</returns>
        private string GetDifficultyString(Difficulty difficulty)
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
        private string GetQuestionTypeString(QuestionType type)
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
        private string GetEncodingString(Encoding encoding) 
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