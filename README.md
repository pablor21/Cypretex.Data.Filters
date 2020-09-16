# NETCORE DYNAMIC FILTER

[![NuGet](https://img.shields.io/nuget/v/Cypretex.Data.Filters?maxAge=259200&style=flat)](https://www.nuget.org/packages/Cypretex.Data.Filters/)

This package allows you to add dynamic filters to an IQueryable collections, it works with any collection that supports IQueryable (IEnumerable local collections, EF, etc)

You can parse the filter from a json string and apply it to the source, or construct the filter manually.

## Install

```
PM> Install-Package Cypretex.Data.Filters
```

## Features

- Supports nullable and non-nullable fields in a transparent way
- Supports chain of where clauses (AND, OR, NOT)
- Supports OrderBy statments
- Supports Skip and Take for pagination
- Supports Include filters (EF only) => Soon!
- Supports Select of subset of properties (deep over the child objects and collections) 

## Available Data Types

The fields to query must be of one of the following types (with the nullable versions):
 * DateTime
 * DateTimeOffset
 * TimeSpan
 * bool
 * byte
 * sbyte
 * short
 * ushort
 * int
 * uint
 * long
 * ulong
 * Guid
 * double
 * float
 * decimal
 * char
 * string


~~~ 
You can extend the types adding new data types to AvailableCastTypes in Cypretex.Data.Filters.Parsers.Linq.Utils
~~~

## Available comparators

The following comparators are available:

* EQUALS
* NOT_EQUALS
* GREATHER_THAN
* GREATHER_OR_EQUALS
* LESS_THAN
* LESS_OR_EQUALS
* IS_NULL
* IS_NOT_NULL
* BETWEEN
* NOT_BETWEEN
* IN
* NOT_IN

#### String only comparators
* IS_NULL_OR_EMPTY
* IS_NOT_NULL_OR_EMPTY
* CONTAINS
* NOT_CONTAINS
* STARTS_WITH
* NOT_STARTS_WITH
* ENDS_WITH
* NOT_ENDS_WITH
* EMPTY
* NOT_EMPTY
* REGEX
* NOT_REGEX

## Select a subset of properties (since V1.0.1)

The filter accepts a string for the selection of a subset of properties of objects. The select must be in the form:

```
string select="Id,Name,Child[Id,ChildName,DeepChild[Id]]"
```
The properties of the child objects can be selected with the name of the child property and the subproperties wrapped by [ and ]. ** Ex: Child[Id] **

## Usage

For the examples we will use the following model clases:
```
public class User
{
    public int? Id { get; set; }
    public string Name { get; set; }
    public List<Car> Cars { get; set; } = new List<Car>();
    public User? Parent { get; set; }
    public DateTime BirthDate { get; set; }
}

public class Car
{
    public int? Id { get; set; }
    public string Make { get; set; }
    public int Year { get; set; }
    public User Owner { get; set; }
}
```

Now we can create a list of users with cars
```
var col = new List<User>();
int carIndex = 1;
for (int i = 1; i < 100; i++)
{
    User u = new User()
    {
        Id = i,
        Name = "User " + i,
        Parent = i > 2 ? col.First() : null,
        BirthDate = DateTime.UtcNow.AddDays(-i),
    };

    for (var j = 1; j < 11; j++)
    {
        carIndex++;
        u.Cars.Add(new Car
        {
            Id = carIndex,
            Make = "Make " + carIndex,
            Year = carIndex,
            Owner = u,
        }); ;
    }

    col.Add(u);
}
```

Create a filter and appy it

```
IFilter f = new Filter();
f.AndWhere(new WhereCondition()
{
    Field = "Name",
    Comparator = WhereComparator.BETWEEN,
    Value = new string[]{"user 2", "user 3"},
}).AndWhere(new WhereCondition(){
    Field="Id",
    Comparator= WhereComparator.REGEX,
    Value=@"User (\d+)"
}).Order("-Id");
var result = col.AsQueryable().Filter<User>(f);
foreach (User u in result)
{
   //do something with the result
}
```

## Usage from Json string

The principal use case for this library is to construct dynamic queries from Json filter representations, for example generated from the frontend in a web app or reading a json file.

You can convert the following json string in the filter using System.Text.Json and apply to a collection.

This is a json representation of a filter
```
{
  "select": "Id,Cars[Id,Owner[Id]]"
  "where": {
    "Field": "Name",
    "Comparator": "EQ",
    "Value": "Test",
    "And": [
      {
        "Field": "Field2",
        "Comparator": "NOT_EQUALS",
        "Value": "Test2"
      }
    ]
  },
  "skip": 20,
  "take": 100
}
```

You can convert the json using the following method:

```
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    IgnoreNullValues = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

};
Filter f = JsonSerializer.Deserialize<Filter>(json, options);

//now you can apply to a collection using col.Filter<T>(f)
```

## TODO
- [X] Add a Include filter
- [-] Make extensive tests