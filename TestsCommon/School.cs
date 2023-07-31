using System.ComponentModel.DataAnnotations;

namespace DimonSmart.TestsCommon;

public class School
{
    public School(string name, Book mainBook)
    {
        Name = name;
        MainBook = mainBook;
    }

    public School()
    {
        Name = string.Empty;
        MainBook = default!;
    }

    [Key] public int Id { get; set; }

    public string Name { get; set; }

    public virtual Book MainBook { get; set; }
}