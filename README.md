# Reactive metaprogramming over HTTP

For NDC 2013. http://ndcoslo.oktaset.com/t-8118


## The basic idea

We host programs at urls using Nancy. We can interact with those programs by submitting data to them and reading the results afterwards.

A program is simply an IObservable. Input data are accepted by means of POST requests. Output data can be read by means of GET requests.

![A program](https://raw.github.com/einarwh/Metarx/master/images/program-io.jpg)

## Program as a web service

The simple face detection program is hosted at the url http://localhost:50935/programs/4.

We can publish data to it by issuing POST requests using curl:

curl -XPOST http://localhost:50935/programs/4/faces -d @confidencedata.json

curl -XPOST http://localhost:50935/programs/4/navdata -d @altitudedata.json

If the program has produced any output data, we can consume it by issuing GET requests:

curl http://localhost:50935/programs/4



## Compiler as a web service

The face detection program isn't terribly versatile, in the sense that it is cumbersome to adapt. It's not agile.

However, we have more interesting programs hosted, like Rosie. Rosie uses Roslyn to compile submitted programs on the fly, and host them at urls of their own. In effect, it produces an IObservable of .NET programs from an IObservable of source code strings. 

In practice, a program submitted to Rosie should have the following behaviour: when executed, it should produce a new IObservable that accepts input and output over HTTP, at a url set up by Nancy. In terms of Rx, we submit programs that act as a Select on the posted data.

Rosie is program 0. We can submit programs to Rosie like this:

curl -XPOST http://localhost:50935/programs/0 --data-binary @facedetection.cs

When we submit a GET request to Rosie, we get a 303 See Other response with a Location header containing the url of the new program. Under the hood, Nancy will inspect the output of Rosie, see that the result is a .NET type implementing an Execute method that takes an IObservable as parameter and returns an IObservable as result, and proceed to set the resulting IObservable up with a url of its own.

The -i flag instructs curl to print the headers of the HTTP response, so we typically do:

curl -i http://localhost:50935/programs/0 

This will give us the url of the program that we can now start sending data to, just like we did before.



## Interpreter as a web service

The face detection program is a pretty simple program to submit to Rosie. Rosie can handle more sophisticated programs too. Say, if you happen to have an interpreter for a programming language available, you can submit that to Rosie and have it hosted by Nancy. Let's assume you or a co-presenter happen to by carrying around the source code for a LISPish interpreter called Nihil. You can submit that to Rosie like this:

curl -XPOST http://localhost:50935/programs/0 --data-binary @Nihil.cs

You'd proceed to GET the url of Nihil like this:

curl -i http://localhost:50935/programs/0

Let's assume that Nihil gets hosted as program 6. Now you can proceed to submit LISPish programs to Nihil, like this:

curl -XPOST http://localhost:50935/programs/6 -d @threadingfh.nil

The threadingfh.nil file is assumed to contain a program that Nihil will understand - in this case we can imagine a Nihil program that does the same thing as facedetection.cs using the threading macro syntax of that particular programming language.


![A program](https://raw.github.com/einarwh/Metarx/master/images/20130606_214924.jpg)


## Compilers all the way down

The astute observer might notice that Rosie is a .NET type that implements an Execute method that takes an IObservable as parameter and returns an IObservable as output. This means that you could very easily submit the source code for Rosie to Rosie. What you get is a new manifestation of Rosie hosted at a new url. And you could keep on doing so for as long as you'd like.


![A program](https://raw.github.com/einarwh/Metarx/master/images/20130606_221017.jpg)
