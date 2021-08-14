﻿using Microsoft.AspNetCore.Authorization;
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
	public class SubmissionsController : ControllerBase
	{
		private readonly IStudentService _studentService;

		public SubmissionsController(IStudentService studentService)
		{
			_studentService = studentService;
		}

		[HttpPost]
		[Authorize(Roles = "student")]
		public async Task<ActionResult> CreateSubmission([FromBody] Submission submission)
		{
			string username = User.FindFirstValue(ClaimTypes.Name);

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