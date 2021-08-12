using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualClassroom.Models;
using VirtualClassroom.Services;

namespace VirtualClassroom.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AssignmentsController : ControllerBase
	{
		private readonly IAssignmentService _assignmentService;

		public AssignmentsController(IAssignmentService assignmentService)
		{
			_assignmentService = assignmentService;
		}

		[HttpGet("{assignmentId:length(24)}", Name = "GetAssignment")]
		[Authorize]
		public ActionResult<Assignment> GetAssignment([FromRoute] string assignmentId)
		{
			Assignment assignment = _assignmentService.Get(assignmentId);

			if (assignment == null)
			{
				return NotFound();
			}

			return assignment;
		}
	}
}
