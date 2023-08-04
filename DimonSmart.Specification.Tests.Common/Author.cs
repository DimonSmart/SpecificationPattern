namespace DimonSmart.Tests.Common;

public class Author
{
    public Author(string name)
    {
        Name = name;
    }

    public int Id { get; set; }
    public string Name { get; set; }
}