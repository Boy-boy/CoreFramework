using Core.Ddd.Domain.Entities;
using System;

namespace EntityFrameworkCore.Api.Entities
{
    public class Student : AggregateRoot
    {
        public Student(string name, int age)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Age = age;
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
