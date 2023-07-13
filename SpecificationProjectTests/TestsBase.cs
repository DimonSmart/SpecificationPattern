namespace SpecificationProjectTests
{
    public class TestsBase
    {
        protected static readonly Student Alex22 = new(22, "Alex");
        protected static readonly Student Sofia20 = new(20, "Sofia");
        protected static readonly Student Alex30 = new(30, "Alex");
        protected static readonly IQueryable<Student> Students = new List<Student> { Alex22, Sofia20, Alex30 }.AsQueryable();
    }
}