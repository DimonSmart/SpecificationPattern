using System.ComponentModel.DataAnnotations;

namespace DimonSmart.Specification.Tests.Common;

public sealed class Student
{
    public Student(int age, string name, School school, IEnumerable<Book> books)
    {
        Age = age;
        Name = name;
        School = school;
        Books = books.ToList();
    }

    public Student()
    {
        Name = string.Empty;
        School = default!;
        Books = new List<Book>();
    }

    [Key] public int Id { get; set; }

    public int Age { get; set; }

    public string Name { get; set; }

    public School School { get; set; }

    public List<Book> Books { get; set; }
}