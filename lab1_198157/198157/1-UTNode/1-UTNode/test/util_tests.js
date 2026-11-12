var expect = require('chai').expect;
var Util = require('../src/util');

var utility = new Util();

describe('util-tests', function() 
{
	it('should pass this canary test', function() 
	{
		expect(true).to.eql(true);
	});
	
	it('should return proper invitation', function() 
	{
		var hello = utility.Hello("all");
		expect(hello).to.eql("Helloooo to all");
	});
});