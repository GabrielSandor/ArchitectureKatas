using Structurizr;
using Structurizr.Client;
using System.IO;
using System.Linq;

namespace MakeTheGrade
{
    class Program
    {
        static void Main(string[] args)
        {
            Workspace workspace = new Workspace("Architectural Kata: Make the Grade",
                "This is a model of the solution to the architectural kata \"Make the Grade\", found at http://nealford.com/katas/list.html.");

            #region Models

            Model model = workspace.Model;

            Person student = model.AddPerson("Student", "A student undertaking a test.");
            Person grader = model.AddPerson("Grader", "A grader assessing the sudents' test answers.");
            Person admin = model.AddPerson("Administrator", "A representative of the state authority in education.");

            SoftwareSystem resultsRepoSubSystem = model.AddSoftwareSystem("Results Repository", "Single location representing all of the test scores across the state.");
            admin.Uses(resultsRepoSubSystem, "Extracts grading reports.");

            SoftwareSystem testsCatalogueSubSystem = model.AddSoftwareSystem("Tests Catalogue", "Authoritative source for tests and grading rules.");
            admin.Uses(testsCatalogueSubSystem, "Administers tests.");

            SoftwareSystem localTestUnitSubSystem = model.AddSoftwareSystem("Local Testing Unit", "On-premises testing subsystem deployed in each testing center.");
            student.Uses(localTestUnitSubSystem, "Undertakes tests");
            grader.Uses(localTestUnitSubSystem, "Grades students");

            localTestUnitSubSystem.Uses(resultsRepoSubSystem, "Push results (batch)");
            localTestUnitSubSystem.Uses(testsCatalogueSubSystem, "Pull tests");

            Container resultsRepo = resultsRepoSubSystem.AddContainer("Results Database",
                "Single location representing all of the test scores across the state.", "Microsoft Azure SQL Database");
            resultsRepo.AddTags("Database");
            Container reportingService = resultsRepoSubSystem.AddContainer("Reporting Service",
                "A reporting system to know which students have taken the tests and what score they received.", "ASP.NET single page app");
            reportingService.Uses(resultsRepo, "Reads from");

            Container testsCatalogueService = testsCatalogueSubSystem.AddContainer("Catalogue Service",
                "Web API provider of latest test and grading rules.", "ASP.NET Web API microservice");
            Container testsCatalogueFrontEnd = testsCatalogueSubSystem.AddContainer("Catalogue Front End",
                "Front end application for administrators managing tests.", "ASP.NET MVC web app");
            Container testsRepo = testsCatalogueSubSystem.AddContainer("Tests Repository",
                "Stores test questions, answers and grading rules.", "NoSQL DBMS");
            testsRepo.AddTags("Database");
            testsCatalogueFrontEnd.Uses(testsRepo, "Reads from and writes to");
            testsCatalogueService.Uses(testsRepo, "Reads from");

            Container locatTestResultsStorage = localTestUnitSubSystem.AddContainer("Local Tests Storage",
                "Stores test answers and grades for students from local testing unit.", "MySQL database");
            locatTestResultsStorage.AddTags("Database");
            Container testingApp = localTestUnitSubSystem.AddContainer("Testing application",
                "Allows students to undertake tests and graders to assess them.", "AngularJS web app");
            Container testResultQueue = localTestUnitSubSystem.AddContainer("Test results queue",
                "Asynchronous queue on which an event is placed when a student finishes a test, so that a grader can be notified.", "RabbitMQ");
            testResultQueue.AddTags("Queue");
            Container evaluator = localTestUnitSubSystem.AddContainer("Evaluator",
                "Retrieves a finished test, grades the multiple choice answers and gives manual control to graders", "");
            Container synchronizer = localTestUnitSubSystem.AddContainer("Synchronizer",
                "Uploads local test results to central repository", "WCF service");
            synchronizer.Uses(resultsRepoSubSystem, "Push results (batch)");
            synchronizer.Uses(locatTestResultsStorage, "Read from");

            testingApp.Uses(testResultQueue, "Undertake test");
            testingApp.Uses(testsCatalogueSubSystem, "Pull tests");
            testResultQueue.Uses(evaluator, "Notify grader");
            evaluator.Uses(locatTestResultsStorage, "Store and read results");
            evaluator.Uses(testsCatalogueSubSystem, "Pull tests");

            #endregion

            #region Views

            var layoutWorkspace = WorkspaceUtils.LoadWorkspaceFromJson(new FileInfo("layout.json"));

            ViewSet viewSet = workspace.Views;
            SystemContextView contextView = viewSet.CreateSystemContextView(testsCatalogueSubSystem, "SystemContext", "A system used for standardized testing across all public school systems grades 3-12.");
            contextView.AddAllSoftwareSystems();
            contextView.AddAllPeople();
            contextView.AddNearestNeighbours(testsCatalogueSubSystem);
            contextView.CopyLayoutInformationFrom(layoutWorkspace.Views.SystemContextViews.FirstOrDefault(x => x.Key == "SystemContext"));

            ContainerView resultsContainerView = viewSet.CreateContainerView(resultsRepoSubSystem, "ResultsContainer", "The container diagram for the Results Repository Subsystem.");
            admin.Uses(reportingService, "Extracts reports");
            resultsContainerView.Add(admin);
            resultsContainerView.AddAllContainers();
            resultsContainerView.CopyLayoutInformationFrom(layoutWorkspace.Views.ContainerViews.FirstOrDefault(x => x.Key == "ResultsContainer"));

            ContainerView testsContainerView = viewSet.CreateContainerView(testsCatalogueSubSystem, "TestsContainers", "The container diagram for the Tests Catalogue Subsystem.");
            admin.Uses(testsCatalogueFrontEnd, "Administers tests");
            testsContainerView.Add(admin);
            testsContainerView.AddAllContainers();
            testsContainerView.CopyLayoutInformationFrom(layoutWorkspace.Views.ContainerViews.FirstOrDefault(x => x.Key == "TestsContainers"));

            ContainerView testUnitContainerView = viewSet.CreateContainerView(localTestUnitSubSystem, "TestUnitsContainers", "The container diagram for the Local Testing Unit Subsystem.");
            student.Uses(testingApp, "Undertake test");
            grader.Uses(evaluator, "Grade test answers");
            testUnitContainerView.Add(student);
            testUnitContainerView.Add(grader);
            testUnitContainerView.AddAllContainers();
            testUnitContainerView.Add(resultsRepoSubSystem);
            testUnitContainerView.Add(testsCatalogueSubSystem);
            testUnitContainerView.CopyLayoutInformationFrom(layoutWorkspace.Views.ContainerViews.FirstOrDefault(x => x.Key == "TestUnitsContainers"));

            Styles styles = viewSet.Configuration.Styles;
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff" });
            styles.Add(new ElementStyle(Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Shape.Person });
            styles.Add(new ElementStyle("Database") { Shape = Shape.Cylinder });
            styles.Add(new ElementStyle("Queue") { Shape = Shape.Ellipse });

            #endregion

            PushWorkspace(workspace);
        }

        private static void PushWorkspace(Workspace workspace)
        {
            StructurizrClient structurizrClient = new StructurizrClient("6bb913da-be9e-4d7c-96f4-ec39a8c9a3a2 ", "cf05575f-e709-481c-a493-e47641aff096");
            structurizrClient.PutWorkspace(36985, workspace);
        }
    }
}
