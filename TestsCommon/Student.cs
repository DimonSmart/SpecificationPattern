namespace TestsCommon;

public record Author(string Name)
{
    public static implicit operator Author(string name)
    {
        return new Author(name);
    }
}

public record School(string Name, Book MainBook)
{
    public static implicit operator School(string name)
    {
        return new School(name);
    }
}




public record Student(int Age, string Name, School School, IEnumerable<Book> Books);

public record Book(string Name, Author Author);