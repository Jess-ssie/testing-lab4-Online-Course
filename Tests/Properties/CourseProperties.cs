using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Online_Course_Enrollment_System_Properties.Domain;
using Online_Course_Enrollment_System_Properties.Tests.Arbitraries;

namespace Online_Course_Enrollment_System_Properties.Tests.Properties
{
    public class CourseProperties
    {
        // P2 · Інваріант
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Enrollment_keeps_course_within_capacity(Course course, Student[] students)
        {
            foreach (var student in students)
            {
                course.Enroll(student);
                course.Students.Count.Should().BeLessThanOrEqualTo(course.Capacity);
                course.FreeSeats.Should().BeInRange(0, course.Capacity);
            }
        }

        // P1 · Туди й назад
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Enroll_then_unenroll_restores_course(Course course, Student[] students, Student student)
        {
            foreach (var enrolled in students)
                course.Enroll(enrolled);

            bool applicable = !course.IsFull && !course.IsEnrolled(student.Id);
            bool ok = true;

            if (applicable)
            {
                var countBefore = course.Students.Count;
                var seatsBefore = course.FreeSeats;
                course.Enroll(student);
                course.Unenroll(student.Id);
                ok = course.Students.Count == countBefore
                    && course.FreeSeats == seatsBefore
                    && !course.IsEnrolled(student.Id);
            }

            return ok.When(applicable);
        }

        // P3 · Ідемпотентність
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Repeated_enrollment_does_not_change_course(Course course, Student student)
        {
            course.Enroll(student).Should().BeTrue();
            var countBefore = course.Students.Count;
            var seatsBefore = course.FreeSeats;

            var enrolledAgain = course.Enroll(new Student(student.Id));

            enrolledAgain.Should().BeFalse();
            course.Students.Count.Should().Be(countBefore);
            course.FreeSeats.Should().Be(seatsBefore);
            course.IsEnrolled(student.Id).Should().BeTrue();
        }

        // P2 · Інваріант
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Full_course_does_not_enroll_another_student(Course course, Student student)
        {
            for (int i = 0; i < course.Capacity; i++)
                course.Enroll(new Student($"Filled{i}"));

            var countBefore = course.Students.Count;
            var enrolled = course.Enroll(student);

            enrolled.Should().BeFalse();
            course.Students.Count.Should().Be(countBefore);
            course.FreeSeats.Should().Be(0);
            course.IsEnrolled(student.Id).Should().BeFalse();
        }

        // P2 · Інваріант: некоректний запис не змінює список студентів
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Enroll_null_throws(Course course, Student[] students)
        {
            foreach (var student in students)
                course.Enroll(student);

            var studentsBefore = course.Students.ToArray();
            Action act = () => course.Enroll(null!);

            act.Should().Throw<ArgumentNullException>();
            course.Students.Should().Equal(studentsBefore);
        }

        // P2 · Інваріант: скасування відсутнього запису не змінює список студентів
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Unenroll_missing_student_throws(Course course, Student[] students, Student student)
        {
            foreach (var enrolled in students)
                course.Enroll(enrolled);

            bool applicable = !course.IsEnrolled(student.Id);
            bool ok = true;

            if (applicable)
            {
                var studentsBefore = course.Students.ToArray();
                Action act = () => course.Unenroll(student.Id);

                act.Should().Throw<InvalidOperationException>();
                ok = course.Students.SequenceEqual(studentsBefore);
            }

            return ok.When(applicable);
        }
    }
}
