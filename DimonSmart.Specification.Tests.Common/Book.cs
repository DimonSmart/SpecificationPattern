using System.ComponentModel.DataAnnotations;

namespace DimonSmart.Specification.Tests.Common;

public sealed class Book
{
    public Book(string name, Author author)
    {
        Name = name;
        Author = author;
    }

    public Book()
    {
        Name = string.Empty;
        Author = default!;
    }

    [Key] public int Id { get; set; }

    public string Name { get; set; }

    public Author Author { get; set; }
}