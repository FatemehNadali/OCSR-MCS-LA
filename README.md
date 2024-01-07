# OCSR-MCS-LA
This program is coded to do the following tasks:

1: Read the data of a certain instance from a text file and solve it by the greedy heuristic, GRASP, and ACS-GRASP algorithms.

2: Generate a random instance with determined characteristis and save its data as a text file.

3: Generate instances in batch mode and save their data as text files

4: Read and solve all instances created in task 3 and save results as Excel files.

USER GUIDE:
The data of instances used in the manuscript and its
	text guide are located in the folder "instances"

Name format of text data : ex[InsNum]_init.txt
	Example:for the data of instnace 4: ex4_init.txt  

In "main" function of "Program.cs" set the value of
	the variable "ch" 1,2,3, or, 4 to choose what task you
	want to run. (corresponding to the number of the above tasks)

Task 1:
Example: the data of a certain instance is in "ex4_init.txt"
	and you want to solve it.  
Copy the data file in folder "bin\debug\"
Assign 1 to the variable "ch", and 4 to "Insnum" (lines 17 & 18)
Run the program.
The results will be printed console window.
You can change the value of the parameters of the alogrithms in 
	"Strategies" function of "Program.cs".

Task 2:
Example: generate a random instance and save it as "ex5_init.txt" 
detrmine characteristics of random instance you want to
	generate, in "main" function of "program.cs".
Assign 2 to "ch", and 5 to "Insnum".
Run the program.
the text data will be created in folder "bin\debug\".

Task 3:
Assign 3 to "ch".
Set the beginning number of InsNum and the total number
	of instances in "BatchInstancesGenerator" function
	of "Progtam.cs".
Also set the characteristics of instances in that function
Run the program.
Text files will be created in folder "bin\debug\".

Task 4:
Assign 4 to "ch".
Create an excel file according to the template "res.xlsx" in
	folder "bin\debug\".
Set the output excel filename (say "res.xlsx"),and  the name of the
	folder (say "fn") containing the data of instances, the beginning
	number of InsNum, the total number of instances,
	and, the total number of runs for each instances, in
	"BatchRun" function of "Program.cs".
Run the program.
The program read the data of instances located in "bin\debug\fn\", solve
	them and write the results in "bin\debug\res.xlsx".
You can change the value of the parameters of the alogrithms in 
	"BatchRun" function of "Program.cs".
 
