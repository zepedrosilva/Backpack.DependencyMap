namespace Backpack.DependencyMap
{
    public class ApplicationArguments
    {
        public string Path { get; set; }
        public string Filter { get; set; }
        public string Exclude { get; set; }
        public bool Recursive { get; set; }
        public bool CleanDatabase { get; set; }
    }
}