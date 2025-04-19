function readPackage(pkg) {
  // Force all packages to use the same version of react and react-dom
  if (pkg.dependencies && (pkg.dependencies.react || pkg.dependencies['react-dom'])) {
    pkg.dependencies.react = '19.1.0';
    pkg.dependencies['react-dom'] = '19.1.0';
  }

  if (pkg.peerDependencies && (pkg.peerDependencies.react || pkg.peerDependencies['react-dom'])) {
    pkg.peerDependencies.react = '19.1.0';
    pkg.peerDependencies['react-dom'] = '19.1.0';
  }

  return pkg;
}

module.exports = {
  hooks: {
    readPackage
  }
}; 