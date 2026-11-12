var expect = require('chai').expect;
var Worker = require('../src/worker');

describe('worker-tests', function() 
{
	var worker;
	
	beforeEach(function(){
		worker = new Worker("Axel");
	});	

	it('should return proper invitation', function() 
	{
		var invitation = worker.Introduce();
		expect(invitation).to.eql("Hello I'm Axel");
	});
	
	it('should report work in proper way', function() 
	{
		var report = worker.DoJob("my job");
		expect(report).to.eql("Worker Axel does my job");
	});
	
});