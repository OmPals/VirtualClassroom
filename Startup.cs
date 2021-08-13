using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VirtualClassroom.Helpers;
using VirtualClassroom.Models;
using VirtualClassroom.Services;

namespace VirtualClassroom
{
	public class Startup
	{
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _configuration;

		public Startup(IWebHostEnvironment env, IConfiguration configuration)
		{
			_env = env;
			_configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<UserDatabaseSettings>(
				_configuration.GetSection(nameof(UserDatabaseSettings)));

			services.AddSingleton<IUserDatabaseSettings>(sp =>
				sp.GetRequiredService<IOptions<UserDatabaseSettings>>().Value);

			services.Configure<AssignmentDatabaseSettings>(
				_configuration.GetSection(nameof(AssignmentDatabaseSettings)));

			services.AddSingleton<IAssignmentDatabaseSettings>(sp =>
				sp.GetRequiredService<IOptions<AssignmentDatabaseSettings>>().Value);

			services.Configure<SubmissionDatabaseSettings>(
				_configuration.GetSection(nameof(SubmissionDatabaseSettings)));

			services.AddSingleton<ISubmissionDatabaseSettings>(sp =>
				sp.GetRequiredService<IOptions<SubmissionDatabaseSettings>>().Value);


			services.AddSingleton<IUserService, UserService>();

			services.AddSingleton<ITutorService, TutorService>();

			services.AddSingleton<IStudentService, StudentService>();

			services.AddSingleton<IAssignmentService, AssignmentService>();

			services.AddSingleton<ISubmissionService, SubmissionService>();

			// configure strongly typed settings objects
			var appSettingsSection = _configuration.GetSection("AppSettings");
			services.Configure<AppSettings>(appSettingsSection);

			// configure jwt authentication
			var appSettings = appSettingsSection.Get<AppSettings>();
			var key = Encoding.ASCII.GetBytes(appSettings.Secret);
			services.AddAuthentication(x =>
			{
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(x =>
			{
				x.Events = new JwtBearerEvents
				{
					OnTokenValidated = async context =>
					{
						ClaimsPrincipal claims = context.Principal;
						string userId = claims.FindFirstValue(ClaimTypes.NameIdentifier);

						IUserService userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
						User user = await userService.GetByIdAsync(userId);

						context.Request.RouteValues.TryGetValue("userId", out object requestedUserId);

						if (user == null || (requestedUserId != null && userId != requestedUserId.ToString()))
						{
							// return unauthorized if user no longer exists
							context.Response.StatusCode = 403;
							context.Fail("Unauthorized");
						}
					}
				};

				x.RequireHttpsMetadata = false;
				x.SaveToken = true;
				x.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false
				};
			});


			services.AddControllers()
				.AddNewtonsoftJson(options => options.UseMemberCasing());
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthentication();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
