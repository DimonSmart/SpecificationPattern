using TestsCommon;

namespace SpecificationProjectTests
{
    public class TestsBase
    {
        protected static readonly Book TheGreatGatsby = new Book("The Great Gatsby", "F. Scott Fitzgerald");
        protected static readonly Book ToKillAMockingbird = new Book("To Kill a Mockingbird", "Harper Lee");
        protected static readonly Book PrideAndPrejudice = new Book("Pride and Prejudice", "Jane Austen");
        protected static readonly Book TheCatcherInTheRye = new Book("The Catcher in the Rye", "J.D. Salinger");
        protected static readonly Book AnimalFarm = new Book("Animal Farm", "George Orwell");
        protected static readonly Book TheLordOfTheRings = new Book("The Lord of the Rings", "J.R.R. Tolkien");
        protected static readonly Book TheAlchemist = new Book("The Alchemist", "Paulo Coelho");
        protected static readonly Book TheKiteRunner = new Book("The Kite Runner", "Khaled Hosseini");
        protected static readonly Book TheHandmaidsTale = new Book("The Handmaid's Tale", "Margaret Atwood");
        protected static readonly Book TheBookThief = new Book("The Book Thief", "Markus Zusak");

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
    }
}