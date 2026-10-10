using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IJobPostingService, JobPostingService>();
builder.Services.AddScoped<IJobSkillService, JobSkillService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IStudentSkillService, StudentSkillService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAlumniService, AlumniService>();
builder.Services.AddScoped<ICertificationService, CertificationService>();
builder.Services.AddScoped<IInternshipService, InternshipService>();
builder.Services.AddScoped<IEmploymentHistoryService, EmploymentHistoryService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
