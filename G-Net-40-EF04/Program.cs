using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace G_Net_40_EF04
{
    #region Enums
    public enum CustomerType { Individual = 1, Business = 2 }
    public enum AccountType { Savings = 1, Current = 2, Business = 3 }
    public enum OwnershipRole { Primary = 1, CoHolder = 2 }
    public enum AccountStatus { Active = 1, Closed = 2 }
    public enum TransactionType { Deposit, Withdrawal, Transfer, Payment }
    #endregion

    #region Entities
    public class Branch
    {
        [Key]
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }

        public virtual Manager Manager { get; set; }
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    }

    public class Manager
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; } 
        public DateTime HireDate { get; set; }

        public string BranchCode { get; set; }
        public virtual Branch Branch { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string NationalId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public CustomerType CustomerType { get; set; }

        public virtual ICollection<CustomerAccount> CustomerAccounts { get; set; } = new List<CustomerAccount>();
    }

    public class Account
    {
        [Key]
        public string AccountNumber { get; set; }
        public AccountType AccountType { get; set; }
        public DateTime OpeningDate { get; set; }
        public decimal CurrentBalance { get; set; }

        public string BranchCode { get; set; }
        public virtual Branch Branch { get; set; }

        public virtual ICollection<CustomerAccount> CustomerAccounts { get; set; } = new List<CustomerAccount>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class CustomerAccount
    {
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        public string AccountNumber { get; set; }
        public virtual Account Account { get; set; }

        public DateTime OwnershipStartDate { get; set; }
        public OwnershipRole OwnershipType { get; set; }
        public AccountStatus AccountStatus { get; set; }
    }

    public class Transaction
    {
        [Key]
        public int TransactionNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Note { get; set; }

        public string AccountNumber { get; set; }
        public virtual Account Account { get; set; }
    }
    #endregion

    #region Data Context
    public class BankDbContext : DbContext
    {
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<CustomerAccount> CustomerAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlServer("Server=.;Database=BankManagementDb;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1-to-1: Branch and Manager
            modelBuilder.Entity<Branch>()
                .HasOne(b => b.Manager)
                .WithOne(m => m.Branch)
                .HasForeignKey<Manager>(m => m.BranchCode);

            // Composite Key for Many-to-Many
            modelBuilder.Entity<CustomerAccount>()
                .HasKey(ca => new { ca.CustomerId, ca.AccountNumber });

            // Data Seeding for Branch
            modelBuilder.Entity<Branch>().HasData(
                new Branch { Code = "CAI-01", Name = "Cairo Main", Address = "Downtown", PhoneNumber = "02123456" },
                new Branch { Code = "ALX-01", Name = "Alexandria", Address = "Corniche", PhoneNumber = "03123456" }
            );

            // FIX: Added PhoneNumber here to satisfy the requirement
            modelBuilder.Entity<Manager>().HasData(
                new Manager { Id = 1, FullName = "Ahmed Hassan", Email = "ahmed@bank.com", PhoneNumber = "01011122233", BranchCode = "CAI-01", HireDate = DateTime.Now },
                new Manager { Id = 2, FullName = "Sara Ali", Email = "sara@bank.com", PhoneNumber = "01233344455", BranchCode = "ALX-01", HireDate = DateTime.Now }
            );
        }
    }
    #endregion

    #region Program Logic
    internal class Program
    {
        static void Main(string[] args)
        {
            using var db = new BankDbContext();

            db.Database.EnsureCreated();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("======= National Bank System =======");
                Console.WriteLine("1) Add Customer");
                Console.WriteLine("2) Open Account");
                Console.WriteLine("3) Update Account Status");
                Console.WriteLine("4) List All Data (Demonstrate Loading Strategies)");
                Console.WriteLine("0) Exit");
                Console.Write("Select: ");

                if (!int.TryParse(Console.ReadLine(), out int choice)) continue;
                if (choice == 0) break;

                switch (choice)
                {
                    case 1: AddCustomer(db); break;
                    case 2: OpenAccount(db); break;
                    case 3: UpdateStatus(db); break;
                    case 4: DemonstrateLoading(db); break;
                }
                Console.WriteLine("\nPress any key...");
                Console.ReadKey();
            }
        }

        static void AddCustomer(BankDbContext db)
        {
            var c = new Customer();
            Console.WriteLine("\n--- Add New Customer ---");

            Console.Write("Name: ");
            c.FullName = Console.ReadLine();

            Console.Write("National ID: ");
            c.NationalId = Console.ReadLine();

            Console.Write("Email: ");
            c.Email = Console.ReadLine();

            Console.Write("Address: ");
            c.Address = Console.ReadLine(); 

            Console.Write("Phone Number: ");
            c.PhoneNumber = Console.ReadLine();

            c.DateOfBirth = DateTime.Now.AddYears(-25);
            c.CustomerType = CustomerType.Individual;

            db.Customers.Add(c);
            db.SaveChanges();

            Console.WriteLine($"\nSuccess! Customer created with ID: {c.Id}");
        }

        static void OpenAccount(BankDbContext db)
        {
            Console.Write("Acc Number: "); string accNum = Console.ReadLine();
            Console.Write("Cust Id: "); int cId = int.Parse(Console.ReadLine());
            Console.Write("Branch Code (CAI-01 / ALX-01): "); string bCode = Console.ReadLine();

            var acc = new Account { AccountNumber = accNum, BranchCode = bCode, OpeningDate = DateTime.Now, CurrentBalance = 0 };
            db.Accounts.Add(acc);
            db.CustomerAccounts.Add(new CustomerAccount { AccountNumber = accNum, CustomerId = cId, OwnershipType = OwnershipRole.Primary, AccountStatus = AccountStatus.Active, OwnershipStartDate = DateTime.Now });
            db.SaveChanges();
            Console.WriteLine("Account Created and Linked.");
        }

        static void UpdateStatus(BankDbContext db)
        {
            Console.Write("Acc Number: "); string accNum = Console.ReadLine();
            Console.Write("Cust Id: "); int cId = int.Parse(Console.ReadLine());
            var link = db.CustomerAccounts.Find(cId, accNum);
            if (link != null)
            {
                link.AccountStatus = AccountStatus.Closed;
                db.SaveChanges();
                Console.WriteLine("Status Updated to Closed.");
            }
            else
            {
                Console.WriteLine("Account link not found.");
            }
        }

        static void DemonstrateLoading(BankDbContext db)
        {
            // 1. Eager Loading
            Console.WriteLine("\n--- Eager Loading (Branch + Manager) ---");
            var branches = db.Branches.Include(b => b.Manager).ToList();
            foreach (var b in branches)
                Console.WriteLine($"Branch: {b.Name} | Manager: {b.Manager?.FullName}");

            // 2. Explicit Loading
            Console.WriteLine("\n--- Explicit Loading (Customer -> Accounts) ---");
            var customer = db.Customers.FirstOrDefault();
            if (customer != null)
            {
                db.Entry(customer).Collection(c => c.CustomerAccounts).Load();
                Console.WriteLine($"Customer {customer.FullName} has {customer.CustomerAccounts.Count} account link(s).");
            }

            // 3. Lazy Loading
            Console.WriteLine("\n--- Lazy Loading (Account -> Branch Name) ---");
            var account = db.Accounts.FirstOrDefault();
            if (account != null)
            {
                // Branch will be loaded automatically when accessed due to proxies
                Console.WriteLine($"Account {account.AccountNumber} belongs to {account.Branch.Name} branch.");
            }
        }
    }
    #endregion
}