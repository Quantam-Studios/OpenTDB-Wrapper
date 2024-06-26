# OpenTDB-Wrapper
An async C# wrapper for the [Open Trivia DB API](https://opentdb.com/api_config.php).
# Add to Project
Via `dotnet`:
```console
dotnet add package OpenTDB-Wrapper --version 1.9.1
```
Via `PackageReference` in your `.csproj` file:
```csproj
<PackageReference Include="OpenTDB-Wrapper" Version="1.9.1" />
```
# Usage
### Setup
As a good rule of thumb, add these `using` statements.
```cs
using OpenTDB; // For fetching questions from the API
using OpenTDB.Enumerators; // For specifying optional paramters
using OpenTDB.Exceptions; // For handling OpenTDB exceptions
using OpenTDB.Models; // For interacting with the Question class
```

Create an instance of `OpenTDB`.
```cs
OpenTDB.OpenTDB openTDB = new();
```
If you want to pass in your own `HttpClient` you can do it like so:
```cs
HttpClient httpClient = new();

OpenTDB.OpenTDB openTDB = new(httpClient);
```
### Using Tokens
> [!WARNING] 
> It is **highly** recommended that you call `InitializeTokenAsync()` immediately after creation of the `OpenTDB` object if you call it at all.

The Open Trivia Database API provides session tokens for ensuring no duplicate questions are retrieved. You can start usage of a token like so:
```cs
await openTDB.InitializeTokenAsync();
// Initializes the session Token.
```
After one of the following happens you will need to call `ResetTokenAsync()`, this will get a new session token: 
- After **6 hours** your session token is automatically deleted by Open Trivia Database. 
- If you ever exhaust all questions in the database.

You will know for certain that you should call this method if you get one of the following exceptions, these indicate your session token is dead:
- `Code 3: Token Not Found | Session Token does not exist.`
- `Code 4: Token Empty | Session Token has returned all possible questions for the specified query. Resetting the Token is necessary.`
```cs
await openTDB.ResetTokenAsync();
// Sets the Token to a newly created one.
```

### Getting Questions
> [!WARNING]
> `GetQuestionsAsync()` should be wrapped in a `try-catch` block as it throws `OpenTDBException` and `ArguementException`.

> [!WARNING]  
> `GetQuestionsAsync()`, should not be called less-than 5 seconds apart due to Open Trivia DB' rate limits.

Use `GetQuestionsAsync()` to get a list of questions. This method has default values of `Category.Any`, `Difficulty.Any`, and `QuestionType.Any`.
```cs
await openTDB.GetQuestionsAsync(10);
// returns a List<Question> with a Count of 10.
```
Here is a more complicated example. If you wanted to get 10 Nature questions, that were of easy difficulty, and in the form of multiple-choice questions, it would look like so:
```cs
await openTDB.GetQuestionsAsync(10, Category.Nature, Difficulty.Easy, QuestionType.MultipleChoice);
// returns a List<Question> with a Count of 10.
```
> [!IMPORTANT]  
> All values in the `Question` class will be encoded in whatever you specified when calling `GetQuestionsWithEncodingAsync()`. You must handle parsing of this data.

Here is a more complicated example. If you wanted to get 10 Nature questions, that were of easy difficulty, in the form of a multiple-choice questions, and had legacy URL encoding it would look like so:
```cs
await openTDB.GetQuestionsWithEncodingAsync(10, Category.Nature, Difficulty.Easy, QuestionType.MultipleChoice, Encoding.LegacyURL);
// returns a List<Question> with a Count of 10.
```
### Question Categories
The `Category` enumerator contains all valid opetions for the "category" value of the API.
| Value               | Description           | URL Request Category Value |
| ------------------- | --------------------- | -------------- |
| `Any`               | Any category           | -              |
| `GeneralKnowledge`  | General Knowledge      | 9              |
| `Books`             | Books                 | 10             |
| `Film`              | Film                  | 11             |
| `Music`             | Music                 | 12             |
| `MusicalsTheatres`  | Musicals & Theatres   | 13             |
| `Television`        | Television            | 14             |
| `VideoGames`        | Video Games           | 15             |
| `BoardGames`        | Board Games           | 16             |
| `Nature`            | Nature                | 17             |
| `Computers`         | Computers             | 18             |
| `Mathematics`       | Mathematics           | 19             |
| `Mythology`         | Mythology             | 20             |
| `Sports`            | Sports                | 21             |
| `Geography`         | Geography             | 22             |
| `History`           | History               | 23             |
| `Politics`          | Politics              | 24             |
| `Art`               | Art                   | 25             |
| `Celebrities`       | Celebrities           | 26             |
| `Animals`           | Animals               | 27             |
| `Vehicles`          | Vehicles              | 28             |
| `Comics`            | Comics                | 29             |
| `Gadgets`           | Gadgets               | 30             |
| `AnimeManga`        | Anime & Manga         | 31             |
| `CartoonsAnimations`| Cartoons & Animations | 32             |

### Question Difficulty
The `Difficulty` enumerator contains all valid options for the "difficulty" value of the API.
| Value       | Description       | Difficulty Value |
| ----------- | ------------------ | ----------------- |
| `Any`       | Any difficulty     | -                 |
| `Easy`      | Easy               | "easy"            |
| `Medium`    | Medium             | "medium"          |
| `Hard`      | Hard               | "hard"            |

### Question Types
The `QuestionType` enumerator contains all valid options for the "type" value of the API.
| Value               | Description           | Type Value  |
| ------------------- | --------------------- | ----------- |
| `Any`               | Any type of question   | -           |
| `MultipleChoice`    | Multiple-choice question | "multiple"  |
| `TrueFalse`         | True or false question  | "boolean"   |

### Question Encoding
The `Encoding` enumerator contains all valid options for the "encoding" value of the API.
| Value         | Description       | Encoding Value |
| ------------- | ------------------ | --------------- |
| `HTML`        | HTML               | -               |
| `LegacyURL`   | Legacy URL         | "urlLegacy"     |
| `URL`         | URL                | "url3986"       |
| `Base64`      | Base64             | "base64"        |

### Question
The `Question` class looks like this:
```cs
public class Question
{
  public string Type { get; set; }
  public string Difficulty { get; set; }
  public string Category { get; set; }
  public string QuestionTitle { get; set; }
  public string CorrectAnswer { get; set; }
  public string[] IncorrectAnswers { get; set; }
}
```

### Category Question Count Lookup
If you need to determine how many questions are in a specific category you can do so with `GetCategoryQuestionTotalsAsync()`\
This can be done with a specific `Category`:
```cs
await GetCategoryQuestionTotalsAsync(Category.Nature);
// returns a CategoryCount object.
```
Or with an integer representing the `Category` ID:
```cs
await GetCategoryQuestionTotalsAsync(17);
// returns a CategoryCount object.
```

### CategoryCount
The `CategoryCount` class looks like this:
```cs
public class CategoryCount
{
  public int CategoryId { get; set; }
  public int TotalQuestions { get; set; }
  public int TotalEasyQuestions { get; set; }
  public int TotalMediumQuestions { get; set; }
  public int TotalHardQuestions { get; set; }
}
```

### Global Question Count Lookup
If you need to find out things like how many questions there are in the entire database you should use `GetGlobalQuestionTotalsAsync()`.
```cs
await GetGlobalQuestionTotalsAsync();
// returns a GlobalCount object.
```

### GlobalCount
The `GlobalCount` class looks like this:
```cs
public class GlobalCount
{
  public int TotalQuestions { get; set; }
  public int TotalPendingQuestions { get; set; }
  public int TotalVerifiedQuestions { get; set; }
  public int TotalRejectedQuestions { get; set; }
  public List<GlobalCategoryCount> Categories { get; set; }
}
```

### API Category Lookup
If you need to find just the categories provided by the API you should use `GetApiCategoriesAsync()` (although, you could always just look at the `Category` enum).
```cs
await GetApiCategoriesAsync()
// returns a List<ApiCategory> object.
```

### ApiCategory
The `ApiCategory` class looks like this:
```cs
public class ApiCategory
{
  public int Id { get; set; }
  public string Name { get; set; }
}
```

# API Coverage
This wrapper has 100% API coverage!

# Legal Stuff
- Open Trivia DB API uses [Creative Commons Attribution-ShareAlike 4.0 International License](https://creativecommons.org/licenses/by-sa/4.0/).
- OpenTDB-Wrapper uses [GPL-3.0 License](LICENSE) and is not affiliated with the Open Trivia Database.






