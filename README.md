Link to POSTMAN collection: https://www.getpostman.com/collections/4c06186b98acd9406577

# API documentation: 

# Authentication endpoint for tutors

## Description
- Authenticates a user as a tutor in virtual classroom system

## Request
- Route: https://localhost:5001/api/tutors/authenticate
- A request payload: `{
    "username": "tutor-1",
    "password": "pass123"
}`

## Response

#### Successful scenario
- On a successful authentication scenario, the user is returned a jwt and the user-id with status code 200 he has been assigned to the system.
- In this case, tutor-id is returned
- Example: `{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI2MTE4Y2IyMGQ1MmJiNDU5NmNhMGFiYjIiLCJ1bmlxdWVfbmFtZSI6InR1dG9yLTEiLCJyb2xlIjoidHV0b3IiLCJuYmYiOjE2MjkwMTQ4MTcsImV4cCI6MTYyOTYxOTYxNywiaWF0IjoxNjI5MDE0ODE3LCJpc3MiOiJ2aXJ0dWFsQ2xhc3Nyb29tQmFja2VuZCIsImF1ZCI6InZpcnR1YWxDbGFzc3Jvb21Vc2VyIn0.fQpbXmyloqeCqdlmD7Mx-zvul_EcPnfxd2qptCXCDy4",
    "tutorId": "6118cb20d52bb4596ca0abb2"
}`

#### Failure scenario
- When not specifying the fields, system returns with 400 status code.

# Authentication endpoint for tutors

## Description
- Authenticates a user as a student in virtual classroom system

## Request
- Route: https://localhost:5001/api/students/authenticate
- Payload: `{
    "username": "student-3",
    "password": "pass123"
}`

## Response

#### Successful scenario
- On a successful authentication scenario, the user is returned a jwt and the user-id with status code 200 he has been assigned to the system.
- In this case, student-id is returned
- Example: `{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI2MTE4ZGVhOWQ4MGExNjg0ZmY3NDE3ZDkiLCJ1bmlxdWVfbmFtZSI6InN0dWRlbnQtMiIsInJvbGUiOiJzdHVkZW50IiwibmJmIjoxNjI5MDE5ODE3LCJleHAiOjE2Mjk2MjQ2MTcsImlhdCI6MTYyOTAxOTgxNywiaXNzIjoidmlydHVhbENsYXNzcm9vbUJhY2tlbmQiLCJhdWQiOiJ2aXJ0dWFsQ2xhc3Nyb29tVXNlciJ9.AV7OT57xACuqzdCKLmD55T13OSAFPbGfys8B1eMTmdM",
    "studentId": "6118dea9d80a1684ff7417d9"
}`

#### Failure scenario
- When not specifying the fields, system returns with 400 status code.

# Assignment CREATE endpoint for tutors

## Description
- A new assignment is created given the details of the assignment in the request. The system sets assignment as SCHEDULED if PublishedAt is a future timestamp, else ONGOING.
- This API is accessible only for the tutors, i.e. JWT got from tutors authentication only is valid.

## Request
- Route: https://localhost:5001/api/assignments 
- A request consists of description, pulishedAt, deadlineDate and list of students. All of the fields are required.
- Example:  `{
    "description": "Usual scenario",
    "publishedAt": "Sun, 15 Aug 2021 06:28:01 GMT",
    "deadLineDate": "Mon, 16 Aug 2021 06:30:51 GMT",
    "students": ["student-1"]
}`

## Response

#### Successful scenario
- On a successful CREATE scenario, the user is returned the same assignment he has created with an id and status with status code 200.
- Example: `{
    "Id": "6118db5eb45b4c9ec38d912b",
    "Description": "Usual scenario",
    "Students": [
        "student-1"
    ],
    "PublishedAt": "2021-08-15T06:28:01Z",
    "DeadlineDate": "2021-08-16T06:30:51Z",
    "Tutor": "tutor-1",
    "Status": "ONGOING"
}`

#### Failure scenario
- When not specifying the fields, system returns with 400 status code.

# Assignment DELETE endpoint for tutors

## Description
- An assignment gets deleted by id
- This API is accessible only for the tutors, i.e. JWT got from tutors authentication only is valid.

## Request
- Route: https://localhost:5001/api/assignments/{assignmentId} 
- The route contains assignment id  to be deleted

## Response

#### Successful scenario
- On a successful DELETE scenario, the user is returned No Content 204 status code.

#### Failure scenario
- If an assignment id does not exist, then returns 404 status code.

# Assignment UPDATE endpoint for tutors

## Description
- An existing assignment is updated given the details of the assignment in the request. 
- Status gets changed according to publishedAt timestamp.
- This API is accessible only for the tutors, i.e. JWT got from tutors authentication only is valid.

## Request
- Route: https://localhost:5001/api/assignments/{assignmentId}
- A request route path contains the assignment id in the end.
- A request consists of description, pulishedAt, deadlineDate and list of students. All of the fields are required. 
- Payload: `{
    "description": "example asignment 1",
    "publishedAt": "Sun, 15 Aug 2021 06:28:01 GMT",
    "deadLineDate": "Mon, 16 Aug 2021 06:30:51 GMT",
    "students": ["student-1"]
}`

## Response

#### Successful scenario
- On a successful UPDATE scenario, the user is returned status code 200.

#### Failure scenario
- When not specifying the fields, system returns with 400 status code.

# Assignment GET endpoint by Id

## Description
- If the API is called by a student, then only the student’s submission
should be returned
- If the API is called by a tutor then all the submissions added for the
assignment by the assigned students should be returned

## Request
- Route:https://localhost:5001/api/assignments/{assignmentId}
- Authorization header with bearer token

## Response

#### Successful scenario
- For Tutor array of submissions: `[
    {
        "Id": "6118d4521876971720581e06",
        "StudentUsername": "student-1",
        "TutorUsername": "tutor-1",
        "AssignmentId": "6118d4521876971720581e05",
        "SubmittedAt": "2021-08-15T08:48:34.373Z",
        "Status": "OVERDUE",
        "Remark": "this is damn remark23"
    }
]`

- For student single submission for that assignmentId: `{
        "Id": "6118d4521876971720581e06",
        "StudentUsername": "student-1",
        "TutorUsername": "tutor-1",
        "AssignmentId": "6118d4521876971720581e05",
        "SubmittedAt": "2021-08-15T08:48:34.373Z",
        "Status": "OVERDUE",
        "Remark": "this is damn remark23"
    }`

#### Failure scenario
- Returns 404 if assignmentId not found or student not assigned to the assignment

# Assignments GET endpoint by Id

## Description
- For a tutor, the feed will return all the assignments created by the tutor
- For students, the feed will return all the assignments assigned to the student.
- The feed will have following filters:
- publishedAt(Assignment published date): Applicable for
student and tutor feed, which can have values
    -SCHEDULED
    -ONGOING
- status(Submission status filter): Applicable for student feed
only which can have values
    -ALL
    -PENDING
    -OVERDUE
    -SUBMITTED

## Request
- Route: https://localhost:5001/api/assignments

- Query params: publishedAt=ONGOING, status=OVERDUE
- Authorization header with bearer token
- Tutor does not have to specify the status parameter

## Response

#### Successful scenario
- For tutor array of assignments: `[
    {
        "Id": "6118d4521876971720581e05",
        "Description": "Usual scenario 2",
        "Students": [
            "student-1"
        ],
        "PublishedAt": "2021-08-15T06:28:01Z",
        "DeadlineDate": "2021-08-16T06:30:51Z",
        "Tutor": "tutor-1",
        "Status": "ONGOING"
    },
    {
        "Id": "6118d99bd957279a0957657d",
        "Description": "Usual scenario",
        "Students": [
            "student-1"
        ],
        "PublishedAt": "2021-08-15T06:28:01Z",
        "DeadlineDate": "2021-08-16T06:30:51Z",
        "Tutor": "tutor-1",
        "Status": "ONGOING"
    },
    {
        "Id": "6118db5eb45b4c9ec38d912b",
        "Description": "Usual scenario",
        "Students": [
            "student-1"
        ],
        "PublishedAt": "2021-08-15T06:28:01Z",
        "DeadlineDate": "2021-08-16T06:30:51Z",
        "Tutor": "tutor-1",
        "Status": "ONGOING"
    }
]`

- For student all the assignments along with their submissions: `[
    {
        "Assignment": {
            "Id": "6118d4521876971720581e05",
            "Description": "Usual scenario 2",
            "Students": [
                "student-1"
            ],
            "PublishedAt": "2021-08-15T06:28:01Z",
            "DeadlineDate": "2021-08-16T06:30:51Z",
            "Tutor": "tutor-1",
            "Status": "ONGOING"
        },
        "Submission": {
            "Id": "6118d4521876971720581e06",
            "StudentUsername": "student-1",
            "TutorUsername": "tutor-1",
            "AssignmentId": "6118d4521876971720581e05",
            "SubmittedAt": "2021-08-15T09:06:34.988Z",
            "Status": "SUBMITTED",
            "Remark": ""
        }
    },
    {
        "Assignment": {
            "Id": "6118d99bd957279a0957657d",
            "Description": "Usual scenario",
            "Students": [
                "student-1"
            ],
            "PublishedAt": "2021-08-15T06:28:01Z",
            "DeadlineDate": "2021-08-16T06:30:51Z",
            "Tutor": "tutor-1",
            "Status": "ONGOING"
        },
        "Submission": {
            "Id": "6118d99bd957279a0957657e",
            "StudentUsername": "student-1",
            "TutorUsername": "tutor-1",
            "AssignmentId": "6118d99bd957279a0957657d",
            "SubmittedAt": "2021-08-15T09:08:54.098Z",
            "Status": "SUBMITTED",
            "Remark": ""
        }
    },
    {
        "Assignment": {
            "Id": "6118db5eb45b4c9ec38d912b",
            "Description": "Usual scenario",
            "Students": [
                "student-1"
            ],
            "PublishedAt": "2021-08-15T06:28:01Z",
            "DeadlineDate": "2021-08-16T06:30:51Z",
            "Tutor": "tutor-1",
            "Status": "ONGOING"
        },
        "Submission": {
            "Id": "6118db5eb45b4c9ec38d912c",
            "StudentUsername": "student-1",
            "TutorUsername": "tutor-1",
            "AssignmentId": "6118db5eb45b4c9ec38d912b",
            "SubmittedAt": "2021-08-15T09:16:22.276Z",
            "Status": "SUBMITTED",
            "Remark": ""
        }
    }
]`

#### Failure scenario
- Returns 400 if filters are bad.
- For ex: `{
    "message": "Invalid filter for assignments"
}`

# Submission CREATE endpoint for students

## Description
- A new submission for the student gets created specifying a remark 
- This API is accessible only for the students, i.e. JWT got from tutors authentication only is valid.

## Request
- A request route path includes the assignment id for which student wants to make a submission
- Payload consists of a remark field

## Response

#### Successful scenario
- On a successful CREATE scenario, the user is returned with submission id, submission time and a status either SUBMITTED or OVERDUE with status code 201 which contains Location header for where the submission got created.

#### Failure scenario
- Status code 404 when assignment is not found.

#### Exmaple
`{
    "id": "6118db5eb45b4c9ec38d912c",
    "submittedAt": "2021-08-15T09:16:22.2765757Z",
    "status": "SUBMITTED"
}`

