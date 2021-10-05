Environment: .NET core 3.1

Description: The project takes one csv file as an input and outputs a json string in a .json file 

Usage example:
1. Make sure to create a folder called "files" under the project bin directory
2. Put your .csv files in the "files" folder
3. run "CSVtoJSON.exe"
4. type in the csv file name, json file name, and whether the csv file has a header row.
5. The ouput .json file is located in the "files" folder

Example:

	Enter input file name:
	> sample1.csv
	Enter output file name:
	> sample1.json
	Is first row a header row?(true/false):
	> true


Run time:

	O(n) to read and O(n) the print, where n is the number of characters in a csv file. 
	Regex is used to determine whether a block of characters belongs in the same cell. The complexity can be O(n), where n is the number of characters within one block 
