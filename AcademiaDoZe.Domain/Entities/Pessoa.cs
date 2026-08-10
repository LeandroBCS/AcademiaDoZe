// LEANDRO BATISTA CORDOVA DOS SANTOS

using AcademiaDoZe.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcademiaDoZe.Domain.Entities
{
    public abstract class Pessoa : Entity
    {
        public string Nome { get; protected set; }
        public Cpf Cpf { get; protected set; }
        protected Pessoa(int id, string nome, Cpf cpf) : base(id)
        {
            Nome = nome;
            Cpf = cpf;
        }
    }
}
