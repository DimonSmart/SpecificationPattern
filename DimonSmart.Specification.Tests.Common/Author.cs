namespace DimonSmart.Specification.Tests.Common;

public sealed class Author
{
    public Author(string name)
    {
        Name = name;
    }

    public int Id { get; set; }
    public string Name { get; set; }
}