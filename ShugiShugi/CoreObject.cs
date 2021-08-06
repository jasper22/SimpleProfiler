using System;

namespace ShugiShugi
{
    /// <summary>
    /// <c>CoreObject</c>
    /// </summary>
    public class CoreObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreObject"/> class.
        /// </summary>
        public CoreObject()
        {

        }

        public void Function1()
        {
            for(int iCounter = 0; iCounter < 100; iCounter++)
            {
                Console.WriteLine(iCounter);
            }
        }

        public void Function2()
        {
            for (int iCounter = 0; iCounter < 100; iCounter++)
            {
                Console.WriteLine(iCounter);
            }
        }

        public void Function3()
        {
            for (int iCounter = 0; iCounter < 100; iCounter++)
            {
                Console.WriteLine(iCounter);
            }
        }
    }
}
