namespace TestsCommon;

public class TestsBase
{
    // Authors
    protected static readonly Author FScottFitzgerald = new Author("F. Scott Fitzgerald");
    protected static readonly Author HarperLee = new Author("Harper Lee");
    protected static readonly Author JaneAusten = new Author("Jane Austen");
    protected static readonly Author JDSalinger = new Author("J.D. Salinger");
    protected static readonly Author GeorgeOrwell = new Author("George Orwell");
    protected static readonly Author JRRTolkien = new Author("J.R.R. Tolkien");
    protected static readonly Author PauloCoelho = new Author("Paulo Coelho");
    protected static readonly Author KhaledHosseini = new Author("Khaled Hosseini");
    protected static readonly Author MargaretAtwood = new Author("Margaret Atwood");
    protected static readonly Author MarkusZusak = new Author("Markus Zusak");

    // Books
    protected static readonly Book TheGreatGatsby = new Book("The Great Gatsby", FScottFitzgerald);
    protected static readonly Book ToKillAMockingbird = new Book("To Kill a Mockingbird", HarperLee);
    protected static readonly Book PrideAndPrejudice = new Book("Pride and Prejudice", JaneAusten);
    protected static readonly Book TheCatcherInTheRye = new Book("The Catcher in the Rye", JDSalinger);
    protected static readonly Book AnimalFarm = new Book("Animal Farm", GeorgeOrwell);
    protected static readonly Book TheLordOfTheRings = new Book("The Lord of the Rings", JRRTolkien);
    protected static readonly Book TheAlchemist = new Book("The Alchemist", PauloCoelho);
    protected static readonly Book TheKiteRunner = new Book("The Kite Runner", KhaledHosseini);
    protected static readonly Book TheHandmaidsTale = new Book("The Handmaid's Tale", MargaretAtwood);
    protected static readonly Book TheBookThief = new Book("The Book Thief", MarkusZusak);

    protected static readonly IQueryable<Book> Books = new List<Book> {
            TheGreatGatsby, ToKillAMockingbird, PrideAndPrejudice,
            TheCatcherInTheRye, AnimalFarm, TheLordOfTheRings, TheAlchemist,
            TheKiteRunner, TheHandmaidsTale, TheBookThief }.AsQueryable();

    protected static readonly School HighSchool = new School("High School", TheGreatGatsby);
    protected static readonly School ElementarySchool = new School("Elementary School", ToKillAMockingbird);

    protected static readonly Student Alex22 = new Student(22, "Alex", ElementarySchool, new List<Book> { TheGreatGatsby, ToKillAMockingbird, AnimalFarm });
    protected static readonly Student Sofia20 = new Student(20, "Sofia", HighSchool, new List<Book> { PrideAndPrejudice, TheCatcherInTheRye, TheAlchemist });
    protected static readonly Student Alex30 = new Student(30, "Alex", HighSchool, new List<Book> { TheLordOfTheRings, TheKiteRunner, TheHandmaidsTale, TheBookThief });

    protected static readonly IQueryable<Student> Students = new List<Student> { Alex22, Sofia20, Alex30 }.AsQueryable();

    protected static readonly IQueryable<School> Schools = new List<School> { HighSchool, ElementarySchool }.AsQueryable();

    protected static readonly IQueryable<Author> Authors = new List<Author>(Books.Select(b => b.Author)).AsQueryable();
}