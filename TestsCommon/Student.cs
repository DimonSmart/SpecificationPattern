using System.ComponentModel.DataAnnotations;

namespace DimonSmart.TestsCommon;

public class Student
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

    public virtual School School { get; set; }

    public virtual List<Book> Books { get; set; }
}