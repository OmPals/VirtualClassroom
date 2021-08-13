using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace VirtualClassroom.Models
{
	public class Assignment
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; }
		public string Description { get; set; }
		public List<string> Students { get; set; }
		public DateTime PublishedAt { get; set; }
		public DateTime DeadlineDate { get; set; }
		public string Tutor { get; set; }
		public string Status { get; set; }
	}
}
