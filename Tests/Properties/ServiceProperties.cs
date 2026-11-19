using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Online_Course_Enrollment_System_Properties.Domain;
using Online_Course_Enrollment_System_Properties.Tests.Arbitraries;

namespace Online_Course_Enrollment_System_Properties.Tests.Properties
{
    public class ServiceProperties
    {
        // P5 · Оракул
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Service_enrollment_matches_direct_enrollment(Course course, Student[] students, Student student)
        {
            var viaService = new Course(course.Id, course.Capacity);
            var direct = new Course(course.Id, course.Capacity);

            foreach (var enrolled in students)
            {
                viaService.Enroll(enrolled);
                direct.Enroll(enrolled);
            }

            var repository = new EnrollmentRepository();
            repository.Save(viaService);
            var service = new EnrollmentService(repository);

            var serviceResult = service.Enroll(course.Id, student);
            var directResult = direct.Enroll(student);

            serviceResult.Should().Be(directResult);
            viaService.Students.Should().Equal(direct.Students);
            viaService.FreeSeats.Should().Be(direct.FreeSeats);
        }

        // P6 · Метаморфічна властивість
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Enrollment_increases_student_course_count(Course course, Student[] students, Student student)
        {
            foreach (var enrolled in students)
                course.Enroll(enrolled);

            var repository = new EnrollmentRepository();
            repository.Save(course);
            var service = new EnrollmentService(repository);
            bool applicable = !course.IsFull && !course.IsEnrolled(student.Id);
            bool ok = true;

            if (applicable)
            {
                var countBefore = service.CoursesOf(student.Id);
                var enrolled = service.Enroll(course.Id, student);
                ok = enrolled && service.CoursesOf(student.Id) == countBefore + 1;
            }

            return ok.When(applicable);
        }

        // P2 · Інваріант: повторне створення не змінює збережений курс
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Creating_duplicate_course_throws(Course course)
        {
            var repository = new EnrollmentRepository();
            repository.Save(course);
            var service = new EnrollmentService(repository);

            Action act = () => service.CreateCourse(course.Id, course.Capacity);

            act.Should().Throw<InvalidOperationException>();
            repository.Get(course.Id).Should().BeSameAs(course);
            repository.All().Count.Should().Be(1);
        }
    }
}
