# Selecting Semantically-Resonant Colors for Data Visualization

Semantically-resonant color assignments (such as mapping "ocean" to "blue") are useful in categorical visualizations, and can facilitate value lookup and comparisons. 

This repository contains C# code that assigns a set of categorical concept names to semantically-resonant colors.  
The algorithm makes use of images to determine frequent and discirminative colors for each concept name. It uses Google Image Search to query for relevant images. Project page: [link](http://vis.stanford.edu/papers/semantically-resonant-colors)

Main file: SemanticColors.sln

## Dependencies

* [c3_data.json] (https://github.com/StanfordHCI/c3/tree/master/data/xkcd) - Needed for computing color name distances. Download to some place on your computer
* [Google CustomSearch C# Library v1.3.0] (https://code.google.com/p/google-api-dotnet-client/wiki/APIs#CustomSearch_API) - Add/Update the reference to Google.Apis.dll and Google.Apis.Customsearch.v1.dll to Engine.csproj
* You'll also need to sign up for an API key and set up a [CustomSearch engine](http://www.google.com/cse/all). There's a daily query limit for the free option.
 * Create a new search engine with an initial site url. 
 * Then edit the engine, and remove the site from **Sites to search**. Change the selected search option to **Search the entire web but emphasize included sites**. 
 * Enable **Image Search**. 

The code has been tested on Windows 7.

## Usage

    Usage: SemanticColors [inFile] [outFile] [paramFile] [renderDir]
    
* inFile - file with list of categories to assign colors. See sampleCategories.csv for an example
* outFile - output file where color assignments are saved 
* paramFile - config file. See sampleParams.txt for an example where
 * imageDir - is where images of concept names will be saved
 * cacheDir - is where computed histograms will be saved and re-used
 * jsonFile - is the location of the c3_data.json file
 * apiKey - is your Google CustomSearch API key
 * cx - is your Google CustomSearch engine id
* renderDir (optional)- If included, images of the color assignments and affinity scores for each category are saved in this directory.
 
 
