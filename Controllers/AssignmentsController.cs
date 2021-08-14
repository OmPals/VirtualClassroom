using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
		private readonly ITutorService _tutorService;
		private readonly IStudentService _studentService;

		public AssignmentsController(ITutorService tutorService, IStudentService studentService)
		{
			_tutorService = tutorService;
			_studentService = studentService;
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

		[HttpGet("{assignmentId:length(24)}", Name = "GetAssignment")]
		[Authorize]
		public async Task<ActionResult> GetSubmissionsForAssignment([FromRoute] string assignmentId)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);

			if (User.IsInRole(Enums.Role.tutor.ToString()))
			{
				try
				{
					List<Submission> submissions = await _tutorService.GetSubmissionsByAssignmentTutor(assignmentId, username);

					return Ok(submissions);
				}
				catch (Exception ex)
				{
					return NotFound(new { message = ex.Message });
				}
			}
			else
			{
				try
				{
					Submission submission = await _studentService.GetSubmissionByAssignmentStudentAsync(assignmentId, username);

					return Ok(submission);
				}
				catch (Exception ex)
				{
					return NotFound(new { message = ex.Message });
				}
			}
		}

		[HttpGet]
		[Authorize]
		public async Task<ActionResult> GetAssignmentsByFilter([FromQuery] string filterAssignments, [FromQuery] string filterSubmissions)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);

			if (User.IsInRole(Enums.Role.tutor.ToString()))
			{
				try
				{
					List<Assignment> assignments = await _tutorService.GetAssignmentsByFilter(username, filterAssignments);

					return Ok(assignments);
				}
				catch (Exception ex)
				{
					return BadRequest(new { message = ex.Message });
				}
			}
			else
			{
				try
				{
					List<AssignmentSubmission> assignmentSubmissions = await _studentService.GetAssignmentSubmissionByFilter(username, filterAssignments, filterSubmissions);

					return Ok(assignmentSubmissions);
				}
				catch (Exception ex)
				{
					return BadRequest(new { message = ex.Message });
				}
			}
		}

		[HttpPut("{assignmentId:length(24)}")]
		[Authorize(Roles = "tutor")]
		public async Task<ActionResult> UpdateAssignment([FromRoute] string assignmentId, [FromBody] Assignment assignment)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);
			try
			{
				await _tutorService.UpdateAssignment(assignmentId, assignment, username);

				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpDelete("{assignmentId:length(24)}")]
		[Authorize(Roles = "tutor")]
		public async Task<ActionResult> DeleteAssignment([FromRoute] string assignmentId)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);
			try
			{
				await _tutorService.DeleteAssignment(assignmentId, username);

				return NoContent();
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPost("{assignmentId:length(24)}/submission")]
		[Authorize(Roles = "student")]
		public async Task<ActionResult> CreateSubmission([FromRoute] string assignmentId, [FromBody] Submission submission)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);
			submission.AssignmentId = assignmentId;

			try
			{
				submission = await _studentService.CreateSubmissionAsync(username, submission);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}

			return CreatedAtRoute("GetAssignment", new { assignmentId = submission.Id }, submission);
		}
	}
}
