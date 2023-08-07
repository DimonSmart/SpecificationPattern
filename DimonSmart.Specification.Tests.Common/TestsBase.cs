namespace DimonSmart.Specification.Tests.Common;

public class TestsBase
{
    // Authors
    protected static readonly Author FScottFitzgerald = new("F. Scott Fitzgerald");
    protected static readonly Author HarperLee = new("Harper Lee");
    protected static readonly Author JaneAusten = new("Jane Austen");
    protected static readonly Author JDSalinger = new("J.D. Salinger");
    protected static readonly Author GeorgeOrwell = new("George Orwell");
    protected static readonly Author JRRTolkien = new("J.R.R. Tolkien");
    protected static readonly Author PauloCoelho = new("Paulo Coelho");
    protected static readonly Author KhaledHosseini = new("Khaled Hosseini");
    protected static readonly Author MargaretAtwood = new("Margaret Atwood");
    protected static readonly Author MarkusZusak = new("Markus Zusak");

    // Books
    protected static readonly Book TheGreatGatsby = new("The Great Gatsby", FScottFitzgerald);
    protected static readonly Book ToKillAMockingbird = new("To Kill a Mockingbird", HarperLee);
    protected static readonly Book PrideAndPrejudice = new("Pride and Prejudice", JaneAusten);
    protected static readonly Book TheCatcherInTheRye = new("The Catcher in the Rye", JDSalinger);
    protected static readonly Book AnimalFarm = new("Animal Farm", GeorgeOrwell);
    protected static readonly Book TheLordOfTheRings = new("The Lord of the Rings", JRRTolkien);
    protected static readonly Book TheAlchemist = new("The Alchemist", PauloCoelho);
    protected static readonly Book TheKiteRunner = new("The Kite Runner", KhaledHosseini);
    protected static readonly Book TheHandmaidsTale = new("The Handmaid's Tale", MargaretAtwood);
    protected static readonly Book TheBookThief = new("The Book Thief", MarkusZusak);

    protected static readonly IQueryable<Book> Books = new List<Book>
    {
        TheGreatGatsby, ToKillAMockingbird, PrideAndPrejudice,
        TheCatcherInTheRye, AnimalFarm, TheLordOfTheRings, TheAlchemist,
        TheKiteRunner, TheHandmaidsTale, TheBookThief
    }.AsQueryable();

    protected static readonly School HighSchool = new("High School", TheGreatGatsby);
    protected static readonly School ElementarySchool = new("Elementary School", ToKillAMockingbird);

    protected static readonly Student Alex22 = new(22, "Alex", ElementarySchool,
        new List<Book> { TheGreatGatsby, ToKillAMockingbird, AnimalFarm });

    protected static readonly Student Sofia20 = new(20, "Sofia", HighSchool,
        new List<Book> { PrideAndPrejudice, TheCatcherInTheRye, TheAlchemist });

    protected static readonly Student Alex30 = new(30, "Alex", HighSchool,
        new List<Book> { TheLordOfTheRings, TheKiteRunner, TheHandmaidsTale, TheBookThief });

    protected static readonly IQueryable<Student>
        Students = new List<Student> { Alex22, Sofia20, Alex30 }.AsQueryable();

    protected static readonly IQueryable<School> Schools =
        new List<School> { HighSchool, ElementarySchool }.AsQueryable();

    protected static readonly IQueryable<Author> Authors = new List<Author>(Books.Select(b => b.Author)).AsQueryable();
}