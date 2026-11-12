module.exports = function(name) {
		this.Introduce = function() { return "Hello I'm " + name; }
		this.DoJob = function(task) { return "Worker " + name + " does " + task; }
} 
