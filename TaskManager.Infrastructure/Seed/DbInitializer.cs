using Microsoft.AspNetCore.Identity;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Infrastructure.Seed
{
    public static class DbInitializer
    {
        public static async Task Seed(ApplicationDbContext context, UserManager<User> userManager)
        {

            await context.Database.EnsureCreatedAsync();

            try
            {
                if (!context.Users.Any())
                {

                    var user1 = new User
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        UserName = "johndoe",
                        Email = "john@gmail.com",
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user1, "Password123!");

                    var user2 = new User
                    {
                        FirstName = "Josh",
                        LastName = "Brander",
                        UserName = "josh",
                        Email = "josh@gmail.com",
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user2, "Password123!");

                    var user3 = new User
                    {
                        FirstName = "James",
                        LastName = "Jackson",
                        UserName = "james",
                        Email = "james@gmail.com",
                        EmailConfirmed = true
                    };

                    await userManager.CreateAsync(user3, "Password123!");
                }



                // Seed Projects if none exist
                if (!context.Projects.Any())
                {
                    var josh = await userManager.FindByNameAsync("jbran");
                    var john = await userManager.FindByNameAsync("johndoe");
                    var james = await userManager.FindByNameAsync("james");

                    if (josh != null)
                    {
                        context.Projects.Add(
                            Project.Create(
                                Title.Create("Josh's Project").Value,
                                Description.Create("Project for Josh").Value,
                                josh.Id).Value
                        );

                    }

                    if (james != null)
                    {
                        context.Projects.Add(Project.Create(
                           Title.Create("Jame's Project").Value,
                           Description.Create("Project for James").Value,
                           james.Id
                        ).Value);
                    }


                    if (john != null)
                    {
                        context.Projects.Add(Project.Create(
                           Title.Create("John's Project").Value,
                           Description.Create("Project for John").Value,
                        john.Id
                        ).Value);

                    }

                    await context.SaveChangesAsync();
                }

                if (!context.TodoItems.Any())
                {
                    var projects = context.Projects.ToList();
                    var joshProject = projects.FirstOrDefault(p => p.Title.Value == "Josh's Project");
                    var jamesProject = projects.FirstOrDefault(p => p.Title.Value == "James's Project");
                    var johnProject = projects.FirstOrDefault(p => p.Title.Value == "John's Project");

                    if (joshProject != null)
                    {
                        context.TodoItems.Add(TodoItem.Create(
                            Title.Create("Josh's TodoItem").Value,
                            Description.Create("This is Josh's task").Value,
                            joshProject.OwnerId,
                            joshProject.Id,
                            null,
                            null,
                            null
                        ).Value);
                    }

                    if (johnProject != null)
                    {
                        context.TodoItems.Add(TodoItem.Create(
                            Title.Create("Jack's TodoItem").Value,
                            Description.Create("This is Jack's task").Value,
                            johnProject.OwnerId,
                            johnProject.Id,
                            null,
                            null,
                            null
                        ).Value);
                    }

                    if (jamesProject != null)
                    {
                        context.TodoItems.Add(TodoItem.Create(
                            Title.Create("James's TodoItem").Value,
                            Description.Create("This is James's task").Value,
                            jamesProject.OwnerId,
                            jamesProject.Id,
                            null,
                            null,
                            null
                        ).Value);
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}