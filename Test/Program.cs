using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    using System;
    using System.Linq;

    using SocialNetwork.Data;
    using SocialNetwork.Models;

    class Program
    {
        static void Main()
        {
            var repository = new EfRepository<Post>(new ApplicationDbContext());
            Console.WriteLine(repository.All().ToList());
            //var context = new ApplicationDbContext();
            //var usersRepository = new EfRepository<ApplicationUser>(context);
            //var postsRepository = new EfRepository<Post>(context);

            //var posts = postsRepository.All();
            //var users = usersRepository.All();
            //foreach (var post in posts)
            //{
            //    Console.WriteLine(post.Comments.Count);
            //}

            //var f = new Faggot();
            //Console.WriteLine(f.Gender);

            //Task.Run(() => PrintNum());
            //Task.Run(() => PrintNum());

            //Thread.Sleep(10000);
        }

        private static void PrintNum()
        {
            while (counter < 20)
            {
                Console.WriteLine(counter);
                counter++;
            }   
        }

        private static int counter = 0;

        private static object obj1 = new object();

        private static object obj2 = new object();

        static void Do()
        {
            lock (obj1)
            {
                Console.WriteLine("Doing...");
                Thread.Sleep(1000);
                Print();
            }
        }

        static void Print()
        {
            lock (obj2)
            {
                Console.WriteLine("Printing...");
                Thread.Sleep(1000);
                Do();
            }
        }
    }
}
