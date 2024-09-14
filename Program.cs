using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Class_final_work
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DbManager db = new DbManager();

            var transaction = new Transaction {
                Sum = 100m,
                UserId = 1,
                Categories = new List<Category>() { new Category { Name = "Zoo products"} }
                
            
            };


            //1
            //db.AddTransaction(transaction);

            //2
            //var result = db.GetTransaction(1);

            //3
            //DateTime endDate = DateTime.Now;
            //var result = db.GetIncomeByPeriod(1,DateTime.Now,endDate.AddYears(-2));


            //var result = db.GetExpensesByPeriod(1, DateTime.Now, endDate.AddYears(-2));
            //Console.WriteLine(result);



            //4
            //var result = db.GetTransactionOrderedByExpenses(1);

            //var result = db.GetTransactionsOrderedByIncome(1);
        
        
        
        
        
        }
    }



    public class User
    {
        public int Id { get; set; }


        public int UserSettingsId { get; set; }

        public UserSettings UserSettings { get; set; }


        public List<Transaction> Transactions { get; set; } = new();

    }



    public class UserSettings
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public string HomeAdress { get; set; }

        public int UserId { get; set; }

        public User User { get; set; }
    }


    public class Transaction
    {
        public int Id { get; set; }

        public decimal Sum { get; set; }




        public User User { get; set; }

        public int UserId { get; set; }


        public DateTime Date { get; set; }

        public List<Category> Categories { get; set; } = new();


    }


    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int TransactionId { get; set; }

        public Transaction Transaction { get; set; }



    }



    public class ApplicationContext : DbContext
    {

        public DbSet<User> Users { get; set; }

        public DbSet<UserSettings> UserSettings { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Category> Categories { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasOne(u => u.UserSettings).WithOne(us => us.User).HasForeignKey<UserSettings>(u => u.UserId);


            modelBuilder.Entity<User>().HasMany(u => u.Transactions).WithOne(t => t.User).HasForeignKey(u => u.UserId);

            modelBuilder.Entity<Transaction>().HasOne(t => t.User).WithMany(u => u.Transactions).HasForeignKey(u => u.UserId);

            modelBuilder.Entity<Category>().HasOne(c => c.Transaction).WithMany(t => t.Categories).HasForeignKey(c => c.TransactionId);


        }

    }



    public class DbManager
    {
        public DbContextOptions<ApplicationContext> GetConectionOptions()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var config = builder.Build();
            string connectionString = config.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            return optionsBuilder.UseSqlServer(connectionString).Options;
        }

        public void EnsurePopulation()
        {
            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var usersSettings = new List<UserSettings> {
                    new UserSettings
                    {
                        FullName = "John Doe",
                        HomeAdress = "123 Main St, Kyiv, Ukraine"
                    },
                    new UserSettings
                    {
                        FullName = "Jane Smith",
                        HomeAdress = "456 Oak Ave, Lviv, Ukraine"
                    }
                };


                db.UserSettings.AddRange(usersSettings);




               


                var users = new List<User> {
                    new User { UserSettings = usersSettings[0]},
                    new User { UserSettings = usersSettings[1]}
                };

                db.Users.AddRange(users);


                var transactions = new List<Transaction> {
                    new Transaction
                    {
                        Sum = 100.50m,
                        User = users[0],
                        Date = DateTime.Now
                    },
                    new Transaction
                    {
                        Sum = 200.75m,
                        User = users[1],
                        Date = DateTime.Now
                    }
                };


                db.Transactions.AddRange(transactions);

                var category = new List<Category> { 
                    new Category { Name = "Food",Transaction = transactions[0]},
                    new Category {Name = "Drinks",Transaction = transactions[1] }
                };

                db.Categories.AddRange(category);





                db.SaveChanges();
            }
        }



        public void AddTransaction(Transaction transaction)
        {
            using (ApplicationContext db =new ApplicationContext(GetConectionOptions()))
            {
                db.Transactions.Add(transaction);

                db.SaveChanges();
            }
        }


        public List<Transaction> GetTransaction(int userId)
        {
            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {
                return db.Transactions.Where(t => t.UserId == userId).Select(t => new Transaction {
                    Sum = t.Sum,
                    Date = DateTime.Now,
                    Categories = t.Categories
                    
                }).ToList();
            }
        }

        public decimal GetIncomeByPeriod(int userId,DateTime startDate,DateTime endDate)
        {
            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {
                return db.Transactions.Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate && t.Sum >= 1m).Sum(t=>t.Sum);
                
            }
        }


        public decimal GetExpensesByPeriod(int userId,DateTime startDate,DateTime endDate)
        {
            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {

                return db.Transactions.Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate && t.Sum < 0).Sum(t => t.Sum);
            }
        }

        public List<Transaction> GetTransactionOrderedByExpenses(int userId)
        {
            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {
                return db.Transactions.Where(t=>t.UserId == userId && t.Sum<0).OrderByDescending(t=>t.Sum).ToList();
            }
        }

        public List<Transaction> GetTransactionsOrderedByIncome(int userId)
        {

            using (ApplicationContext db = new ApplicationContext(GetConectionOptions()))
            {
                return db.Transactions.Where(t => t.UserId == userId && t.Sum > 1).OrderByDescending(t => t.Sum).ToList();
            }
        }



        

        
    }

    public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        private static IConfigurationRoot config;

        static ApplicationContextFactory()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            config = builder.Build();
        }

        public ApplicationContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            return new ApplicationContext(optionsBuilder.Options);
        }
    }
}
