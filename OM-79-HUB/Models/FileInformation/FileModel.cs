namespace OM_79_HUB.Models.FileInformation
{
    public class DirectoryModel
    {
        public int HubId { get; set; }
        public int OM79Id { get; set; }   
        public int PJ103Id { get; set; }

        public string DirectoryName { get; set; }
        public List<string> Files { get; set; }
        public List<DirectoryModel> SubDirectories { get; set; }

        public DirectoryModel()
        {
            Files = new List<string>();
            SubDirectories = new List<DirectoryModel>();
        }
    }
}
