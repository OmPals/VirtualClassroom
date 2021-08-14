namespace VirtualClassroom.Models
{
	public class Enums
	{
		public enum Role
		{
			tutor,
			student
		}

		public enum AssignmentStatus
		{
			SCHEDULED,
			ONGOING
		}

		public enum SubmissionStatus
		{
			PENDING,
			SUBMITTED,
			OVERDUE,
			ALL
		}

		public enum ClientRequestStatus
		{
			BadRequest,
			NotFound,
			Forbidden
		}
	}
}
