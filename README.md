# OpenTDB-Wrapper
An async C# wrapper for the [Open Trivia DB API](https://opentdb.com/api_config.php).

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
OpenTDB.OpenTDB openTDB = new(); // This contains an HttpClient and it is good practice to not duplicate it.
```
### Getting Questions
> [!WARNING]
> `RequestAsync()` should be wrapped in a `try-catch` block as it throws `OpenTDBException` and `ArguementException`.

> [!WARNING]  
> `RequestAsync()`, should not be called less-than 5 seconds apart due to Open Trivia DB' rate limits.

Use `RequestAsync()` to get a list of questions. This method has default values of `Category.Any`, `Difficulty.Any`, `QuestionType.Any`, and `Encoding.HTML`.
```cs
await openTDB.RequestAsync(1);
// returns a List<Question> with a Count of 10.
```
Here is a more complicated example. If you wanted to get 10 Nature questions, that were of easy difficulty, in the form of a multiple-choice questions, and had Base64 encoding it would look like so:
```cs
await openTDB.RequestAsync(10, Category.Nature, Difficulty.Easy, QuestionType.MultipleChoice, Encoding.Base64);
// returns a List<Question> with a Count of 10.
```
### Question Categories
The `Category` enumerator contains all valid opetions for the "category" value of `ResponseAsync()`.
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
The `Difficulty` enumerator contains all valid options for the "difficulty" value of `RequestAsync()`.
| Value       | Description       | Difficulty Value |
| ----------- | ------------------ | ----------------- |
| `Any`       | Any difficulty     | -                 |
| `Easy`      | Easy               | "easy"            |
| `Medium`    | Medium             | "medium"          |
| `Hard`      | Hard               | "hard"            |

### Question Types
The `QuestionType` enumerator contains all valid options for the "type" value of `RequestAsync()`.
| Value               | Description           | Type Value  |
| ------------------- | --------------------- | ----------- |
| `Any`               | Any type of question   | -           |
| `MultipleChoice`    | Multiple-choice question | "multiple"  |
| `TrueFalse`         | True or false question  | "boolean"   |

### Question Encoding
The `Encoding` enumerator contains all valid options for the "encoding" value of `RequestAsync()`.
| Value         | Description       | Encoding Value |
| ------------- | ------------------ | --------------- |
| `HTML`        | HTML               | -               |
| `LegacyURL`   | Legacy URL         | "urlLegacy"     |
| `URL`         | URL                | "url3986"       |
| `Base64`      | Base64             | "base64"        |


### Manipulating Questions
> [!IMPORTANT]  
> All values in the `Question` class will be encoded in whatever you sepcified when calling `RequestAsync()`. You must handle parsing of this data.

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
# API Coverage
Currently, this wrapper supports all, but the following (there are plans to support all of these):
| Name                                      | Endpoint                                                  |
| ----------------------------------------- | --------------------------------------------------------- |
| Session tokens                            | -                                                         |
| Category lookup endpoint                  | https://opentdb.com/api_category.php                      |
| Category Question Count endpoint          | https://opentdb.com/api_count.php?category=CATEGORY_ID_HERE |
| Global Question Count endpoint            | https://opentdb.com/api_count_global.php                  |


# Legal Stuff
- Open Trivia DB API uses [Creative Commons Attribution-ShareAlike 4.0 International License](https://creativecommons.org/licenses/by-sa/4.0/).
- OpenTDB-Wrapper uses [GPL-3.0 License](LICENSE) and is not affiliated with the Open Trivia Database.






