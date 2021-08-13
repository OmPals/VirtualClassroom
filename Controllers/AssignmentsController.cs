using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VirtualClassroom.Models;
using VirtualClassroom.Services;

namespace VirtualClassroom.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AssignmentsController : ControllerBase
	{
		private readonly IAssignmentService _assignmentService;
		private readonly ITutorService _tutorService;
		private readonly IStudentService _studentService;

		public AssignmentsController(IAssignmentService assignmentService, ITutorService tutorService, IStudentService studentService)
		{
			_assignmentService = assignmentService;
			_tutorService = tutorService;
			_studentService = studentService;
		}

		[HttpGet("{assignmentId:length(24)}", Name = "GetAssignment")]
		[Authorize]
		public async Task<ActionResult<Assignment>> GetAssignmentAsync([FromRoute] string assignmentId)
		{
			Assignment assignment = await _assignmentService.GetAsync(assignmentId);

			if (assignment == null)
			{
				return NotFound();
			}

			return assignment;
		}

		[HttpPost]
		[Authorize(Roles = "tutor")]
		public async Task<ActionResult<Assignment>> CreateAssignmentAsync([FromBody] Assignment assignmentReq)
		{
			Assignment newAssignment;
			string username = User.FindFirstValue(ClaimTypes.Name);

			try
			{
				newAssignment = await _tutorService.CreateAssignmentAsync(username, assignmentReq);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}

			return CreatedAtRoute("GetAssignment", new { assignmentId = newAssignment.Id }, newAssignment);
		}

		//TODO: Update assignment
		/*[HttpPut("{assignmentId:length(24)}")]
		[Authorize(Roles = "tutor")]
		public async Task<ActionResult> UpdateAssignment([FromRoute] string assignmentId)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);

			// Update
		}*/

	}
}
