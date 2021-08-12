namespace VirtualClassroom.Models
{
	public class SubmissionDatabaseSettings : ISubmissionDatabaseSettings
	{
		public string SubmissionsCollectionName { get; set; }
		public string ConnectionString { get; set; }
		public string DatabaseName { get; set; }
	}

	public interface ISubmissionDatabaseSettings
	{
		string SubmissionsCollectionName { get; set; }
		string ConnectionString { get; set; }
		string DatabaseName { get; set; }
	}
}
