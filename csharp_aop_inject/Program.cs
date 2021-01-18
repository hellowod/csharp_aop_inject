﻿using System;

namespace csharp_aop_inject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("hello world1");

            Program p = new Program();
            p.Add(10, 10);
            p.Sub(10, 100);

            Console.WriteLine("hello world2");
        }

        public int Add(int v1, int v2)
        {
            int r = 0;
            for (int i = 0; i < 100000; i++) {
                r = v1 + v2 + r;
            }
            return r;
        }

        public int Sub(int v1, int v2)
        {
            int r = 0;
            for (int i = 0; i < 1000010; i++) {
                r = v1 - v2 + r;
            }
            return r;
        }
    }
}
