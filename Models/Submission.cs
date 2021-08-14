using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace VirtualClassroom.Models
{
	public class Submission
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public string StudentUsername { get; set; }
		public string TutorUsername { get; set; }
		public string AssignmentId { get; set; }
		public DateTime SubmittedAt { get; set; }
		public string Status { get; set; }
		public string Remark { get; set; }
	}
}
