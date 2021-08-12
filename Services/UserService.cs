using MongoDB.Driver;
using System;
using VirtualClassroom.Helpers;
using VirtualClassroom.Models;
using static VirtualClassroom.Models.Enums;

namespace VirtualClassroom.Services
{
	public interface IUserService
	{
		User Authenticate(string username, string password, Role role);
		User Create(User user, string password);
		User GetById(string userId);
	}

	public class UserService : IUserService
	{
		private readonly IMongoCollection<User> _users;

		public UserService(IUserDatabaseSettings settings)
		{
			var client = new MongoClient(settings.ConnectionString);
			var database = client.GetDatabase(settings.DatabaseName);

			_users = database.GetCollection<User>(settings.UsersCollectionName);
		}
		public User GetByUsername(string username) =>
			_users.Find<User>(user => user.Username == username).FirstOrDefault();

		public User GetById(string id) =>
			_users.Find<User>(user => user.Id == id).FirstOrDefault();

		public User Create(User user)
		{
			_users.InsertOne(user);
			return user;
		}

		public void UpdateOne(string id, User userIn) =>
			_users.ReplaceOne(user => user.Id == id, userIn);

		public void Remove(string id) =>
			_users.DeleteOne(user => user.Id == id);

		public User Authenticate(string username, string password, Role role)
		{
			User reqUser = GetByUsername(username);

			// check if username exists
			if (reqUser == null)
			{
				User user = new User()
				{
					Username = username,
					Role = role.ToString()
				};

				user = Create(user, password);

				return user;
			}
			else
			{
				// check if password is correct
				if (!VerifyPasswordHash(password, reqUser.PasswordHash, reqUser.PasswordSalt))
					return null;

				return reqUser;
			}
		}

		public User Create(User user, string password)
		{
			if (string.IsNullOrWhiteSpace(password))
				throw new Exception("password required");

			User user1 = GetByUsername(user.Username);
			if (user1 != null)
			{
				return user1;
			}

			CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

			user.PasswordHash = passwordHash;
			user.PasswordSalt = passwordSalt;

			return Create(user);
		}

		private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
		{
			if (password == null) throw new ArgumentNullException("password");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
			if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
			if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

			using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
			{
				var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
				for (int i = 0; i < computedHash.Length; i++)
				{
					if (computedHash[i] != storedHash[i]) return false;
				}
			}

			return true;
		}

		private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
		{
			if (password == null) throw new ArgumentNullException("password");
			if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

			using var hmac = new System.Security.Cryptography.HMACSHA512();
			passwordSalt = hmac.Key;
			passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
		}
	}
}
