using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMatchAI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    middle_name = table.Column<string>(type: "varchar(100)", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "varchar(255)", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", nullable: false),
                    role = table.Column<string>(type: "varchar(20)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: true),
                    is_active = table.Column<bool>(type: "bool", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alumni",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program = table.Column<string>(type: "varchar(30)", nullable: false),
                    graduation_year = table.Column<DateOnly>(type: "date", nullable: false),
                    current_employer = table.Column<string>(type: "varchar(100)", nullable: true),
                    current_position = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alumni", x => x.id);
                    table.ForeignKey(
                        name: "FK_alumni_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "varchar(80)", nullable: false),
                    industry = table.Column<string>(type: "varchar(80)", nullable: false),
                    website = table.Column<string>(type: "varchar(500)", nullable: true),
                    company_description = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "varchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employers", x => x.id);
                    table.ForeignKey(
                        name: "FK_employers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id_number = table.Column<string>(type: "varchar(30)", nullable: false),
                    program = table.Column<string>(type: "varchar(100)", nullable: false),
                    year_level = table.Column<string>(type: "varchar(20)", nullable: false),
                    gwa = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    graduation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    resume_file_path = table.Column<string>(type: "varchar(100)", nullable: true),
                    resume_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employment_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alumni_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    position = table.Column<string>(type: "varchar(100)", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    responsibilities = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employment_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_employment_histories_alumni_alumni_id",
                        column: x => x.alumni_id,
                        principalTable: "alumni",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    category = table.Column<string>(type: "varchar(20)", nullable: false),
                    ontology_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "varchar(500)", nullable: true),
                    AlumniId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_skills_alumni_AlumniId",
                        column: x => x.AlumniId,
                        principalTable: "alumni",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_postings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "varchar(50)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    requirements = table.Column<string>(type: "varchar(150)", nullable: false),
                    preferred_skills = table.Column<string>(type: "varchar(150)", nullable: true),
                    min_experience_years = table.Column<int>(type: "int", nullable: false),
                    min_salary = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    max_salary = table.Column<decimal>(type: "numeric(9,2)", nullable: true),
                    location = table.Column<string>(type: "varchar(100)", nullable: false),
                    employment_type = table.Column<string>(type: "varchar(20)", nullable: false),
                    posted_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    deadline = table.Column<DateTime>(type: "timestamp", nullable: true),
                    is_active = table.Column<bool>(type: "bool", nullable: false),
                    views = table.Column<int>(type: "int", nullable: false),
                    applications = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_postings", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_postings_employers_employer_id",
                        column: x => x.employer_id,
                        principalTable: "employers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    issuing_authority = table.Column<string>(type: "varchar(100)", nullable: false),
                    date_issued = table.Column<DateTime>(type: "timestamp", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp", nullable: true),
                    verification_url = table.Column<string>(type: "varchar(500)", nullable: true),
                    credential_id = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_certifications_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "internships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "varchar(100)", nullable: false),
                    position = table.Column<string>(type: "varchar(50)", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    skills_gained = table.Column<string>(type: "varchar(200)", nullable: true),
                    supervisor = table.Column<string>(type: "varchar(150)", nullable: true),
                    supervisor_email = table.Column<string>(type: "varchar(150)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internships", x => x.id);
                    table.ForeignKey(
                        name: "FK_internships_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proficiency_level = table.Column<int>(type: "int", nullable: false),
                    years_of_experience = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_skills_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required = table.Column<bool>(type: "bool", nullable: false),
                    min_proficiency_level = table.Column<int>(type: "int", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_skills", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_skills_job_postings_job_posting_id",
                        column: x => x.job_posting_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_score = table.Column<double>(type: "numeric(5,2)", nullable: false),
                    employability_score = table.Column<double>(type: "numeric(5,2)", nullable: true),
                    match_explanation = table.Column<string>(type: "text", nullable: true),
                    feature_contributions = table.Column<string>(type: "text", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    was_viewed = table.Column<bool>(type: "boolean", nullable: true),
                    was_applied = table.Column<bool>(type: "boolean", nullable: true),
                    was_hired = table.Column<bool>(type: "boolean", nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_results_job_postings_job_posting_id",
                        column: x => x.job_posting_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_match_results_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "placement_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placement_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    placement_status = table.Column<string>(type: "varchar(30)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placement_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_placement_records_job_postings_job_posting_id",
                        column: x => x.job_posting_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_placement_records_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alumni_user_id",
                table: "alumni",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certifications_student_id",
                table: "certifications",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_employers_user_id",
                table: "employers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employment_histories_alumni_id",
                table: "employment_histories",
                column: "alumni_id");

            migrationBuilder.CreateIndex(
                name: "IX_internships_student_id",
                table: "internships",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_employer_id",
                table: "job_postings",
                column: "employer_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_skills_job_posting_id_skill_id",
                table: "job_skills",
                columns: new[] { "job_posting_id", "skill_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_skills_skill_id",
                table: "job_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_results_job_posting_id",
                table: "match_results",
                column: "job_posting_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_results_student_id_job_posting_id",
                table: "match_results",
                columns: new[] { "student_id", "job_posting_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_placement_records_job_posting_id",
                table: "placement_records",
                column: "job_posting_id");

            migrationBuilder.CreateIndex(
                name: "IX_placement_records_student_id_job_posting_id",
                table: "placement_records",
                columns: new[] { "student_id", "job_posting_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skills_AlumniId",
                table: "skills",
                column: "AlumniId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_name",
                table: "skills",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_skills_skill_id",
                table: "student_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_skills_student_id_skill_id",
                table: "student_skills",
                columns: new[] { "student_id", "skill_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_student_id_number",
                table: "students",
                column: "student_id_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_user_id",
                table: "students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certifications");

            migrationBuilder.DropTable(
                name: "employment_histories");

            migrationBuilder.DropTable(
                name: "internships");

            migrationBuilder.DropTable(
                name: "job_skills");

            migrationBuilder.DropTable(
                name: "match_results");

            migrationBuilder.DropTable(
                name: "placement_records");

            migrationBuilder.DropTable(
                name: "student_skills");

            migrationBuilder.DropTable(
                name: "job_postings");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "employers");

            migrationBuilder.DropTable(
                name: "alumni");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
